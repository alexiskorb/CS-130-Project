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
		public const float TICK_RATE = 0f;

		// Tracks the game state.
		public GameClient m_game;
		// Queue of client commands. 
		private Queue<Netcode.CmdType> m_cmdQueue = new Queue<Netcode.CmdType>();
		// Client snapshots. 
		private Netcode.ClientHistory m_clientHistory;
		// The UDP Client. 
		private UdpClient m_client = new UdpClient();
		// The tick function sends client commands at the specified tick rate.
		private PeriodicFunction m_sendTick;
		// The main thread work queue.
		private Queue<Netcode.MainThreadWork> m_mainWork = new Queue<Netcode.MainThreadWork>();
		// Client's current seqno.
		private uint m_seqno = 0;

		void Start()
		{
			// Begin listening for packets.
			m_client.BeginReceive(ReceiveCallback, m_client);

			// Initialize tick function.
			m_sendTick = new PeriodicFunction(ConnectToServer, TICK_RATE);

			// @doc Client history must be initialized with a snapshot! 
			m_clientHistory = new Netcode.ClientHistory(new Netcode.PlayerSnapshot(m_seqno, 0, m_game.GetMainPlayer().GetComponent<Transform>().position));
		}

		void Tick()
		{
			// @test Send snapshots to the server. 
			Vector3 position = m_game.GetMainPlayer().GetComponent<Transform>().position;
			Netcode.PlayerSnapshot snapshot = new Netcode.PlayerSnapshot(m_seqno++, m_game.GetServerId(), position);
			m_clientHistory.PutSnapshot(snapshot);

			SendPacket(Netcode.PlayerSnapshot.Serialize(snapshot));
			// @endtest 
		}

		void Update()
		{
			while (m_mainWork.Count > 0) {
				Netcode.MainThreadWork work = m_mainWork.Dequeue();
				work.Invoke();
			}

			// TODO: Add commands to the command queue and send them to the server.
			m_sendTick.Run();
		}

		// @func ReceiveCallback
		// @desc Asynchronous callback for receiving packets. Discards packets not from the server.
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
			Netcode.Connect connect = Netcode.Connect.Deserialize(buf);
			m_game.NetEvent(connect);
			m_sendTick = new PeriodicFunction(Tick, TICK_RATE);
			ClientLog("Server connection request received! My Server ID is " + m_game.GetServerId());
		}

		void ProcessDisconnect(byte[] buf)
		{
			Netcode.Disconnect disconnect = Netcode.Disconnect.Deserialize(buf);
			if (disconnect.m_serverId == m_game.GetServerId()) {
				// @TODO: Admittedly, this is a little harsh. Network events like these should be passed 
				// to the game logic for it to decide what to do. 
				OnDestroy();
			} else {
				m_game.KillEntity(disconnect.m_serverId);
			}
		}

		void ProcessSnapshot(byte[] buf)
		{
			Netcode.PlayerSnapshot snapshot = Netcode.PlayerSnapshot.Deserialize(buf);
			if (snapshot.m_serverId == m_game.GetServerId()) {
				// Check if the client's player state and server state are out of sync.
				if (!m_clientHistory.Reconcile(snapshot)) {
					ClientLog("OUT OF SYNC.");
					m_clientHistory.PutSnapshot(snapshot);
					m_game.PutSnapshot(snapshot);
				}
			} else {
				m_game.PutSnapshot(snapshot);
			}
		}

		// @func ConnectToServer
		// @desc Sends a connect packet to the server. 
		void ConnectToServer()
		{
			Netcode.Connect connectPacket = new Netcode.Connect(m_seqno, m_game.GetServerId());
			byte[] buf = Netcode.Connect.Serialize(connectPacket);
			SendPacket(buf);
		}

		void SendPacket(byte[] dgram)
		{
			m_client.Send(dgram, dgram.Length, SERVER_IP, SERVER_PORT);
		}

		void OnDestroy()
		{
			Netcode.Disconnect disconnect = new Netcode.Disconnect(m_seqno, m_game.GetServerId());
			SendPacket(Netcode.Disconnect.Serialize(disconnect));
			m_client.Close();
		}

		public void ClientLog(string message)
		{
			Debug.Log("<Client> " + message);
		}
	}
}