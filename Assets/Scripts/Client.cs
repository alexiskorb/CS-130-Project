using UnityEngine;
using System.Collections.Generic;
using FpsNetcode;

namespace FpsClient {
	// @doc Change MySnapshot to whatever Snapshot your game uses.
	using MySnapshot = Netcode.Snapshot;

	// @class Client
	// @desc Performs client-side prediction and server synchronization. 
	public class Client : FpsNetwork {
		// The rate at which updates are sent to the server. 
		public const float TICK_RATE = 0f;
		// Address of the server.
		public Netcode.ClientAddress m_serverAddr = new Netcode.ClientAddress("127.0.0.1", 9000);
		// The client-side game logic.
		public GameClient m_game;
		// Queue of client commands. 
		private Queue<Netcode.CmdType> m_cmdQueue = new Queue<Netcode.CmdType>();
		// Client snapshots. 
		private Netcode.ClientHistory<MySnapshot> m_clientHistory;
		// The tick function sends client commands at the specified tick rate.
		private PeriodicFunction m_tick;
		// Client's current seqno.
		private uint m_seqno = 0;

		private delegate uint NewSeqnoDel();
		NewSeqnoDel m_newSeqno;

		void Start()
		{
			RegisterPacket(Netcode.PacketType.CONNECT, ProcessConnect);
			RegisterPacket(Netcode.PacketType.DISCONNECT, ProcessDisconnect);
			RegisterPacket(Netcode.PacketType.SNAPSHOT, ProcessSnapshot);
			InitUdp();

			// Initialize tick function to the "try to connect" function. 
			m_tick = new PeriodicFunction(ConnectToServer, 2f);

			// Initialize client history. It doesn't matter what the initial snapshot is on the 
			// client side because if it's wrong it will be corrected by the server.
			m_clientHistory = new Netcode.ClientHistory<MySnapshot>(
				new MySnapshot(m_newSeqno(), m_game.GetServerId(), m_game.GetMainPlayer()));

			m_newSeqno = new NewSeqnoDel(GetSeqno);

			m_tick.RunNow();
		}

		void Tick()
		{
			// @test Send snapshots to the server. 
			GameObject mainPlayer = m_game.GetMainPlayer();
			MySnapshot snapshot = new MySnapshot(m_newSeqno(), m_game.GetServerId(), mainPlayer);
			m_clientHistory.PutSnapshot(snapshot);

			SendPacket(m_serverAddr, snapshot);
			// @endtest 
		}

		void Update()
		{
			while (m_mainWork.Count > 0) {
				Netcode.MainThreadWork work = m_mainWork.Dequeue();
				if (work != null)
					work.Invoke();
			}

			m_tick.Run();
		}

		void ProcessConnect(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			// Connected to server - start seqno counting. 
			m_newSeqno = new NewSeqnoDel(NewSeqno);
			// Begin sending updates to server. 
			m_tick = new PeriodicFunction(Tick, TICK_RATE);
			// Alert game to connection event. 
			Netcode.Connect connect = Serializer.Deserialize<Netcode.Connect>(buf);
			m_game.NetEvent(connect);

			ClientLog("Server connection request received. My Server ID is " + m_game.GetServerId());
		}

		void ProcessDisconnect(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			Netcode.Disconnect disconnect = Serializer.Deserialize<Netcode.Disconnect>(buf);
			m_game.NetEvent(disconnect);
		}

		// @func ProcessSnapshot
		// @desc If the snapshot was intended for the main player, and the client and 
		// server are in agreement, the Network Event is ignored.
		void ProcessSnapshot(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			MySnapshot snapshot = Serializer.Deserialize<MySnapshot>(buf);

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
			SendPacket(m_serverAddr, connectPacket);
		}

		// @func OnDestroy
		// @desc Disconnect from the server and close the connection. 
		void OnDestroy()
		{
			Netcode.Disconnect disconnect = new Netcode.Disconnect(m_newSeqno(), m_game.GetServerId());
			SendPacket(m_serverAddr, disconnect);
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

		// @func ShouldDiscard
		// @desc Drop UDP packets that aren't from the server. 
		public override bool ShouldDiscard(Netcode.ClientAddress clientAddr, Netcode.Packet header)
		{
			return !(m_serverAddr.m_ipAddress == clientAddr.m_ipAddress &&
				m_serverAddr.m_port == clientAddr.m_port);
		}
	}
}