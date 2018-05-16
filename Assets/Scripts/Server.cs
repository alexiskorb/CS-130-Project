using UnityEngine;
using System.Collections.Generic;
using FpsNetcode;

// @TODO Commands, reliable connect/disconnect

namespace FpsServer {
	// @alias Change MySnapshot to whatever Snapshot your game uses.
	using MySnapshot = Netcode.Snapshot;
	// @alias The mapping between client address and client history
	using ClientHistoryMapping = Dictionary<Netcode.ClientAddress, Netcode.ClientHistory<Netcode.Snapshot>>;
	// @alias The mapping between client address and server ID.
	using ServerIdMapping = Dictionary<Netcode.ClientAddress, int>;

	// @class Server
	// @desc The authoritative server.
	public class Server : FpsNetwork {
		private const int SERVER_PORT = 9000;

		// The server-side game manager.
		public GameServer m_game;
		// Snapshots sent by all clients. 
		private ClientHistoryMapping m_clients = new ClientHistoryMapping();
		// Mapping from address to server ID. 
		private ServerIdMapping m_serverIds = new ServerIdMapping();

		void Start()
		{
			RegisterPacket(Netcode.PacketType.CONNECT, ProcessConnect);
			RegisterPacket(Netcode.PacketType.DISCONNECT, ProcessDisconnect);
			RegisterPacket(Netcode.PacketType.SNAPSHOT, ProcessSnapshot);
			InitUdp(SERVER_PORT);
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
					SendPacket(clientAddr, disconnect);
					DisconnectClient(clientAddr);
					break;
				}

				MySnapshot snapshot = history.GetMostRecentSnapshot();
				BroadcastPacket(snapshot);
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
				GameObject newPlayer = m_game.NetEvent(Serializer.Deserialize<Netcode.Connect>(buf));
				ServerLog("Creating player with Server ID " + newPlayer.GetInstanceID());
				MySnapshot initialSnapshot = new MySnapshot(0, newPlayer.GetInstanceID(), newPlayer);
				Netcode.ClientHistory<MySnapshot> clientHistory = new Netcode.ClientHistory<MySnapshot>(initialSnapshot);
				m_clients.Add(clientAddr, clientHistory);
				m_serverIds.Add(clientAddr, initialSnapshot.m_serverId);
				// Send CONNECT ack.
				Netcode.Connect connectAck = new Netcode.Connect(clientHistory.GetSeqno(), initialSnapshot.m_serverId);
				SendPacket(clientAddr, connectAck);
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
				ServerLog("Snapshot with seqno " + snapshot.m_seqno + " was received out of order.");
				return;
			}

			m_clients[clientAddr].PutSnapshot(snapshot);
			m_game.NetEvent(snapshot);
		}

		// @func BroadcastPacket
		// @desc Broadcasts the packet to all connected clients.
		void BroadcastPacket<T>(T packet)
		{
			byte[] buf = Serializer.Serialize(packet);
			foreach (var clientAddr in m_clients.Keys) {
				SendPacket(clientAddr, buf);
			}
		}

		void OnDestroy()
		{
			m_udp.Close();
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
			BroadcastPacket(disconnect);

			ServerLog("Disconnecting Client. " + clientAddr.Print());
		}

		public void ServerLog(string message)
		{
			Debug.Log("<Server> " + message);
		}

		public override bool ShouldDiscard(Netcode.ClientAddress clientAddr, Netcode.Packet header)
		{
			if (header.m_type != Netcode.PacketType.CONNECT && !m_clients.ContainsKey(clientAddr)) {
				ServerLog("Client must establish a connection before sending. " + clientAddr.Print());
				return true;
			}

			return false;
		}
	}

}