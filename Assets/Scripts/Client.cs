using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using FpsNetcode;

namespace FpsClient {
	// @class Client
	public class Client : MonoBehaviour {
		// Server IP address.
		public const string SERVER_IP = "127.0.0.1";
		// Server port.
		public const int SERVER_PORT = 9000;
		// The rate at which updates are sent to the server. 
		public const float TICK_RATE = 1 / 30;

		// The client's unique Server ID. 
		public int m_serverId = -1;
		// Tracks the game state. 
		public GameClient m_game;
		// Queue of client commands. 
		private Queue<Netcode.CmdType> m_cmdQueue = new Queue<Netcode.CmdType>();
		// Client snapshots. 
		private Netcode.ClientHistory m_clientHistory;
		// The UDP Client. 
		private UdpClient m_client = new UdpClient();
		// Flag for whether the client is connected to the server.
		private bool m_isConnected = false;
		// The tick function sends client commands at the specified tick rate.
		private PeriodicFunction m_sendTick;
		// The main thread work queue.
		private Queue<Netcode.MainThreadWork> m_mainWork = new Queue<Netcode.MainThreadWork>();
		// Client's current seqno. 
		private uint m_seqno = 0;
		// Current snapshot of the client's main player state. 
		private Netcode.PlayerSnapshot m_snapshot;

		void Start()
		{
			// Begin listening for packets.
			m_client.BeginReceive(ReceiveCallback, m_client);

			Netcode.PlayerSnapshot initialState = new Netcode.PlayerSnapshot(0, m_serverId, m_game.GetMainPlayer().GetComponent<Transform>().position);
			m_clientHistory = new Netcode.ClientHistory(initialState);

			// Initialize tick function.
			m_sendTick = new PeriodicFunction(Tick, TICK_RATE);
		}

		void Tick()
		{
			// @test Send snapshots to the server. 
			SendPacket(Netcode.PlayerSnapshot.Serialize(m_snapshot));
			// @endtest 
		}

		void Update()
		{
			Vector3 position = m_game.GetMainPlayer().GetComponent<Transform>().position;
			Netcode.PlayerSnapshot snapshot = new Netcode.PlayerSnapshot(m_seqno++, m_serverId, position);
			m_snapshot = snapshot;
			m_clientHistory.PutSnapshot(snapshot);

			while (m_mainWork.Count > 0) {
				Netcode.MainThreadWork work = m_mainWork.Dequeue();
				work.Invoke();
			}

			// TODO: Add commands to the command queue and send them to the server.
			if (!m_isConnected)
				ConnectToServer();
			else
				m_sendTick.Run();
		}

		void ReceiveCallback(IAsyncResult asyncResult)
		{
			IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
			byte[] buf = m_client.EndReceive(asyncResult, ref remoteEndPoint);
			m_client.BeginReceive(ReceiveCallback, m_client);

			if (remoteEndPoint.Port != SERVER_PORT || remoteEndPoint.Address.ToString() != SERVER_IP) {
				ClientLog("Packet not from server!");
				return;
			}

			// @doc Unity is not a thread-safe game engine: Calls to the Unity API have to be from the
			// main thread or else the engine will spit out an error. Most of packet processing is therefore done from the main thread.
			Netcode.MainThreadWork work = () => {
				ProcessPacket(buf, remoteEndPoint);
			};

			m_mainWork.Enqueue(work);
		}

		void ProcessPacket(byte[] buf, IPEndPoint endPoint)
		{
			Netcode.PacketHeader header = Netcode.PacketHeader.Deserialize(buf);
			Netcode.ClientAddress serverAddr = new Netcode.ClientAddress(endPoint.Address.ToString(), endPoint.Port);

			switch (header.m_type) {
				case Netcode.PacketType.CONNECT:
					ProcessConnect(buf);
					break;
				case Netcode.PacketType.CLIENT_SNAPSHOT:
					ProcessSnapshot(buf);
					break;
				case Netcode.PacketType.DISCONNECT:
					ProcessDisconnect(buf);
					break;
				default:
					ClientLog("Packet type unknown.");
					break;
			}
		}

		void ProcessConnect(byte[] buf)
		{
			Netcode.ConnectPacket connect = Netcode.ConnectPacket.Deserialize(buf);
			m_serverId = connect.m_serverId;
			m_isConnected = true;
			ClientLog("Server connection request received! My Server ID is " + m_serverId);
		}

		void ProcessDisconnect(byte[] buf)
		{
		}

		void ProcessSnapshot(byte[] buf)
		{
			Netcode.PlayerSnapshot snapshot = Netcode.PlayerSnapshot.Deserialize(buf);
			if (snapshot.m_serverId == m_serverId) {
				// Check if the client's main player and server state are out of sync.
				if (!m_clientHistory.Reconcile(snapshot)) {
					ClientLog("OUT OF SYNC.");
					// TODO: To generalize more, the main player should be able to
					// be updated from m_game.PutSnapshot(). When Connect is called, store the main
					GameObject mainPlayer = m_game.GetMainPlayer();
					Netcode.ApplySnapshot(ref mainPlayer, snapshot);
				} else
					ClientLog("GOOD.");
			} else {
				m_game.PutSnapshot(snapshot);
			}
		}

		void ConnectToServer()
		{
			Netcode.ConnectPacket connectPacket = new Netcode.ConnectPacket(0, 0);
			byte[] buf = Netcode.ConnectPacket.Serialize(connectPacket);
			SendPacket(buf);
		}

		void SendPacket(byte[] dgram)
		{
			m_client.Send(dgram, dgram.Length, SERVER_IP, SERVER_PORT);
		}

		void OnDestroy()
		{
			m_client.Close();
		}

		public void ClientLog(string message)
		{
			Debug.Log("<Client> " + message);
		}
	}
}