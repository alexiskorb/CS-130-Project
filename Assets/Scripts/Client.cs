using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using FpsNetcode;

namespace FpsClient {
	// @doc Change MySnapshot to whatever Snapshot your game uses.
	using MySnapshot = Netcode.Snapshot;

	// @class Client
	// @desc Performs client-side prediction and server synchronization. 
	// Both of these are meant to be completely transparent to the game. 
	public class Client : MonoBehaviour {
		// Server IP address.
		public const string SERVER_IP = "127.0.0.1";
		// Server port.
		public const int SERVER_PORT = 9000;
		// The rate at which updates are sent to the server. 
		public const float TICK_RATE = 0f;

		// This is the client-side game.
		public GameClient m_game;
		// Queue of client commands. 
		private Queue<Netcode.CmdType> m_cmdQueue = new Queue<Netcode.CmdType>();
		// Client snapshots. 
		private Netcode.ClientHistory<MySnapshot> m_clientHistory;
		// The UDP Client. 
		private UdpClient m_client = new UdpClient();
		// The tick function sends client commands at the specified tick rate.
		private PeriodicFunction m_sendTick;
		// The main thread work queue.
		private Queue<Netcode.MainThreadWork> m_mainWork = new Queue<Netcode.MainThreadWork>();
		// Client's current seqno.
		private uint m_seqno = 0;

		private delegate uint NewSeqnoDel();
		NewSeqnoDel m_newSeqno;

		void Start()
		{
			// Begin listening for packets.
			m_client.BeginReceive(ReceiveCallback, m_client);

			// Initialize tick function to the "try to connect" function. 
			m_sendTick = new PeriodicFunction(ConnectToServer, 2f);

			// Initialize client history. It doesn't matter what the initial snapshot is on the 
			// client side because if it's wrong it will be corrected by the server.
			m_clientHistory = new Netcode.ClientHistory<MySnapshot>(
				new MySnapshot(NewSeqno(), m_game.GetServerId(), m_game.GetMainPlayer()));

			m_newSeqno = new NewSeqnoDel(GetSeqno);

			m_sendTick.RunNow();
		}

		void Tick()
		{
			// @test Send snapshots to the server. 
			GameObject mainPlayer = m_game.GetMainPlayer();
			MySnapshot snapshot = new MySnapshot(m_newSeqno(), m_game.GetServerId(), mainPlayer);
			m_clientHistory.PutSnapshot(snapshot);

			SendPacket(snapshot.Serialize());
			// @endtest 
		}

		void Update()
		{
			while (m_mainWork.Count > 0) {
				Netcode.MainThreadWork work = m_mainWork.Dequeue();
				if (work != null)
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
				ClientLog("Packet not from server.");
				return;
			}

			Netcode.MainThreadWork work = () => {
				ProcessPacket(buf, remoteEndPoint);
			};

			m_mainWork.Enqueue(work);
		}

		void ProcessPacket(byte[] buf, IPEndPoint endPoint)
		{
			Netcode.PacketHeader header = Netcode.PacketHeader.Deserialize(buf);

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
			// Connected to server - start seqno counting. 
			m_newSeqno = new NewSeqnoDel(NewSeqno);
			// Begin sending updates to server. 
			m_sendTick = new PeriodicFunction(Tick, TICK_RATE);
			// Alert game to connection event. 
			Netcode.Connect connect = new Netcode.Connect(buf);
			m_game.NetEvent(connect);

			ClientLog("Server connection request received. My Server ID is " + m_game.GetServerId());
		}

		void ProcessDisconnect(byte[] buf)
		{
			Netcode.Disconnect disconnect = new Netcode.Disconnect(buf);
			m_game.NetEvent(disconnect);
		}

		// @func ProcessSnapshot
		// @desc If the snapshot was intended for the main player, and the client and 
		// server are in agreement, the Network Event is ignored and 
		void ProcessSnapshot(byte[] buf)
		{
			MySnapshot snapshot = new MySnapshot(buf);

			// Check if the client's state and server state are out of sync.
			if (snapshot.m_serverId == m_game.GetServerId()) {
				if (!m_clientHistory.Reconcile(snapshot)) {
					ClientLog("Client is out of sync with the server -- reconciling");
					m_game.NetEvent(snapshot);
				} 
			} else 
				m_game.NetEvent(snapshot);
		}

		// @func ConnectToServer
		// @desc Sends a connect packet to the server. This gets called every tick until the game connects.
		void ConnectToServer()
		{
			Netcode.Connect connectPacket = new Netcode.Connect(m_newSeqno(), m_game.GetServerId());
			byte[] buf = connectPacket.Serialize();
			SendPacket(buf);
		}

		// @func SendPacket
		// @desc Sends a datagram packet to the server. 
		void SendPacket(byte[] dgram)
		{
			m_client.Send(dgram, dgram.Length, SERVER_IP, SERVER_PORT);
		}

		// @func OnDestroy
		// @desc Disconnect from the server and close the connection. 
		void OnDestroy()
		{
			Netcode.Disconnect disconnect = new Netcode.Disconnect(m_newSeqno(), m_game.GetServerId());
			SendPacket(disconnect.Serialize());
			m_client.Close();
		}

		// @func NewSeqno
		// @desc Returns the current seqno and increments it. 
		uint NewSeqno()
		{
			uint seqno = m_seqno++;
			return seqno;
		}

		// @func GetSeqno
		// @desc Return the current seqno of the client. 
		uint GetSeqno()
		{
			return m_seqno;
		}

		void ClientLog(string message)
		{
			Debug.Log("<Client> " + message);
		}
	}
}