using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using FpsNetcode;

namespace FpsServer {
	// @alias Change MySnapshot to whatever Snapshot your game uses.
	using MySnapshot = Netcode.Snapshot;
	// @alias The mapping between client address and client history
	using ClientHistoryMapping = Dictionary<Netcode.ClientAddress, Netcode.ClientHistory<Netcode.Snapshot>>;
	// @alias The mapping between client address and server ID.
	using ServerIdMapping = Dictionary<Netcode.ClientAddress, int>;

	// @class Server
	// @desc The authoritative server.
	public class Server : MonoBehaviour {
		private const int SERVER_PORT = 9000;

		// The server-side game manager.
		public GameServer m_game;
		// The UDP server. 
		private UdpClient m_server;
		// Work queue.
		private Queue<Netcode.MainThreadWork> m_mainWork = new Queue<Netcode.MainThreadWork>();
		// Snapshots sent by all clients. 
		private ClientHistoryMapping m_clients = new ClientHistoryMapping();
		// Mapping from address to server ID. 
		private ServerIdMapping m_serverIds = new ServerIdMapping();

		void Start()
		{
			// Initialize server.
			m_server = new UdpClient(SERVER_PORT);
			// Begin listening for packets.
			m_server.BeginReceive(ReceiveCallback, m_server);	
		}

		// @func Update
		// @desc Every update we do all the work in the work queue, then propagate updates to all connected clients.
		void Update()
		{
			while (m_mainWork.Count > 0) {
				Netcode.MainThreadWork work = m_mainWork.Dequeue();
				if (work != null)
					work.Invoke();
			}

			foreach (var client in m_clients) {
				Netcode.ClientHistory<MySnapshot> history = client.Value;
				Netcode.ClientAddress clientAddr = client.Key;

				history.IncrTimeSinceLastAck(Time.deltaTime);

				if (history.GetTimeSinceLastAck() >= Netcode.ClientHistory<MySnapshot>.CLIENT_TIMEOUT) {
					ServerLog("Disconnecting Client. " + clientAddr.Print());
					Netcode.Disconnect disconnect = new Netcode.Disconnect(history.GetSeqno(), m_serverIds[clientAddr]);
					m_game.NetEvent(disconnect);
					SendPacket(clientAddr, Serializer.Serialize(disconnect));
					DisconnectClient(clientAddr);
					break;
				}

				MySnapshot snapshot = history.GetMostRecentSnapshot();
				BroadcastPacket(Serializer.Serialize(snapshot));
			}
		}

		// @func ReceiveCallback
		// @desc The asynchronous callback used to receive datagrams.
		public void ReceiveCallback(IAsyncResult asyncResult)
		{
			IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
			byte[] buf = m_server.EndReceive(asyncResult, ref remoteEndPoint);
			m_server.BeginReceive(ReceiveCallback, m_server);

			// @doc Unity is not a thread-safe game engine: Calls to the Unity API have to be from the
			// main thread or else the engine will spit out an error. Most of packet processing is therefore done from the main thread. 
			Netcode.MainThreadWork work = () => {
				ProcessPacket(buf, remoteEndPoint);
			};

			m_mainWork.Enqueue(work);
		}

		public void ProcessPacket(byte[] buf, IPEndPoint endPoint)
		{
			Netcode.Packet header = Serializer.Deserialize<Netcode.Packet>(buf);
			Netcode.ClientAddress clientAddr = new Netcode.ClientAddress(endPoint.Address.ToString(), endPoint.Port);

			if (header.m_type == Netcode.PacketType.CONNECT) {
				ProcessConnect(clientAddr, buf);
				return;
			}

			// If there is a new client, it should have sent a connect packet already.
			if (!m_clients.ContainsKey(clientAddr)) {
				ServerLog("Client must establish a connection before sending. " + clientAddr.Print());
				return;
			}

			switch (header.m_type) {
				case Netcode.PacketType.SNAPSHOT:
					ProcessSnapshot(clientAddr, buf);
					break;
				case Netcode.PacketType.COMMAND:
					ServerLog("Client sent a command! Seqno " + header.m_seqno);
					break;
				case Netcode.PacketType.DISCONNECT:
					ProcessDisconnect(clientAddr, buf);
					break;
				default:
					ServerLog("Packet type unknown.");
					break;
			}
		}

		// @TODO
		public void ProcessClientCmd()
		{
		}

		public void ProcessConnect(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			// If this is a new client, add it to the client connection table, otherwise discard the packet. 
			if (!m_clients.ContainsKey(clientAddr)) {
				GameObject playerObject = m_game.SpawnPlayer();
				ServerLog("Creating player with Server ID " + playerObject.GetInstanceID());
				MySnapshot initialSnapshot = new MySnapshot(0, playerObject.GetInstanceID(), playerObject);
				Netcode.ClientHistory<MySnapshot> clientHistory = new Netcode.ClientHistory<MySnapshot>(initialSnapshot);
				m_clients.Add(clientAddr, clientHistory);
				m_serverIds.Add(clientAddr, initialSnapshot.m_serverId);
				// Send CONNECT ack.
				Netcode.Connect connectAck = new Netcode.Connect(clientHistory.GetSeqno(), initialSnapshot.m_serverId);
				SendPacket(clientAddr, Serializer.Serialize(connectAck));
			} else
				ServerLog(clientAddr.Print() + " is already connected.");
		}

		public void ProcessDisconnect(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			Netcode.Disconnect disconnect = Serializer.Deserialize<Netcode.Disconnect>(buf);
			m_game.NetEvent(disconnect);
			DisconnectClient(clientAddr);
		}

		public void ProcessSnapshot(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			MySnapshot snapshot = Serializer.Deserialize<MySnapshot>(buf);
			Netcode.ClientHistory<MySnapshot> history = m_clients[clientAddr];

			// Check the seqno, discarding old snapshots.
			if (snapshot.m_seqno < history.GetSeqno()) {
				ServerLog("Packet with seqno " + snapshot.m_seqno + " was received out of order.");
				return;
			}

			m_clients[clientAddr].PutSnapshot(snapshot);
			// Check snapshot data.
			m_game.NetEvent(snapshot);
		}

		// @func BroadcastPacket
		// @desc Broadcasts the packet to all connected clients.
		void BroadcastPacket(byte[] buf)
		{
			foreach (var clientAddr in m_clients.Keys) {
				SendPacket(clientAddr, buf);
			}
		}

		void SendPacket(Netcode.ClientAddress addr, byte[] buf)
		{
			m_server.Send(buf, buf.Length, addr.m_ipAddress, addr.m_port);
		}

		void OnDestroy()
		{
			m_server.Close();
		}

		// @func DisconnectClient
		// @desc Disconnects the client, deleting all player history from the server.
		public void DisconnectClient(Netcode.ClientAddress clientAddr)
		{
			int serverId = m_serverIds[clientAddr];
			uint seqno = m_clients[clientAddr].GetSeqno();

			m_serverIds.Remove(clientAddr);
			m_clients.Remove(clientAddr);

			Netcode.Disconnect disconnect = new Netcode.Disconnect(seqno, serverId);
			BroadcastPacket(Serializer.Serialize(disconnect));

			ServerLog("Disconnecting Client. " + clientAddr.Print());
		}

		public void ServerLog(string message)
		{
			Debug.Log("<Server> " + message);
		}
	}

}