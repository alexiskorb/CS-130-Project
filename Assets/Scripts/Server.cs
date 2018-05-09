using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using FpsNetcode;

namespace FpsServer {
	// @class Server
	// @desc The authoritative server.
	public class Server : MonoBehaviour {
		// The server-side game manager.
		public GameServer m_game;
		// Server port.
		private const int SERVER_PORT = 9000;
		// The server 
		private UdpClient m_server;
		private Queue<Netcode.MainThreadWork> m_mainWork = new Queue<Netcode.MainThreadWork>();
		private Dictionary<Netcode.ClientAddress, Netcode.ClientHistory> m_clients = new Dictionary<Netcode.ClientAddress, Netcode.ClientHistory>();

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
				work.Invoke();
			}

			foreach (var client in m_clients) {
				Netcode.ClientHistory history = client.Value;
				Netcode.ClientAddress addr = client.Key;
				
				history.IncrTimeSinceLastAck(Time.deltaTime);

				if (history.GetTimeSinceLastAck() >= Netcode.ClientHistory.CLIENT_TIMEOUT) {
					ServerLog("Disconnecting client: <" + addr.ipAddress + ", " + addr.port + ">");
					Netcode.PacketHeader disconnectPacket = new Netcode.PacketHeader(Netcode.PacketType.DISCONNECT, history.GetSeqno());
					SendPacket(addr, Netcode.PacketHeader.Serialize(disconnectPacket));
					m_clients.Remove(addr);
					break;
				}

				Netcode.PlayerSnapshot snapshot = history.GetMostRecentSnapshot();
				foreach (var clientAddr in m_clients.Keys) {
					SendPacket(clientAddr, Netcode.PlayerSnapshot.Serialize(snapshot));
				}
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
			Netcode.PacketHeader header = Netcode.PacketHeader.Deserialize(buf);
			Netcode.ClientAddress clientAddr = new Netcode.ClientAddress(endPoint.Address.ToString(), endPoint.Port);

			if (header.m_type == Netcode.PacketType.CONNECT) {
				ProcessConnect(clientAddr);
				return;
			}

			// If there is a new client, it should have sent a connect packet already.
			if (!m_clients.ContainsKey(clientAddr)) {
				ServerLog("Client must establish a connection before sending.\nIP: " + clientAddr.ipAddress + " Port: " + clientAddr.port);
				return;
			}

			Netcode.ClientHistory history = m_clients[clientAddr];

			// Check the seqno, discarding old packets.
			if (header.m_seqno < history.GetSeqno()) {
				ServerLog("Packet with seqno " + header.m_seqno + " was received out of order.");
				return;
			}

			switch (header.m_type) {
				case Netcode.PacketType.CLIENT_SNAPSHOT:
					ProcessSnapshot(clientAddr, buf);
					break;
				case Netcode.PacketType.CLIENT_CMD:
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

		public void ProcessConnect(Netcode.ClientAddress clientAddr)
		{
			// If this is a new client, add it to the client connection table, otherwise discard the packet. 
			if (!m_clients.ContainsKey(clientAddr)) {
				Netcode.PlayerSnapshot initialSnapshot = m_game.SpawnPlayer();
				ServerLog("Creating player with Server ID " + initialSnapshot.m_serverId);
				Netcode.ClientHistory clientHistory = new Netcode.ClientHistory(initialSnapshot);
				m_clients.Add(clientAddr, clientHistory);
				// Send CONNECT ack. 
				SendPacket(clientAddr, Netcode.ConnectPacket.Serialize(new Netcode.ConnectPacket(0, initialSnapshot.m_serverId)));
			} else
				ServerLog(clientAddr.ipAddress + " " + clientAddr.port + " is already connected.");
		}

		// @TODO
		public void ProcessDisconnect(Netcode.ClientAddress clientAddr, byte[] buf)
		{
		}

		public void ProcessSnapshot(Netcode.ClientAddress addr, byte[] buf)
		{
			Netcode.PlayerSnapshot snapshot = Netcode.PlayerSnapshot.Deserialize(buf);
			try {
				m_game.PutSnapshot(snapshot);
				m_clients[addr].PutSnapshot(snapshot);
			} catch (Exception) {
				ServerLog("Invalid Server ID: " + snapshot.m_serverId);
			}
		}

		void SendPacket(Netcode.ClientAddress addr, byte[] buf)
		{
			m_server.Send(buf, buf.Length, addr.ipAddress, addr.port);
		}

		void OnDestroy()
		{
			m_server.Close();
		}

		public void ServerLog(string message)
		{
			Debug.Log("<Server> " + message);
		}
	}

}