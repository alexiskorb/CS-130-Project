using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace FpsClient {
	// @doc Change MySnapshot to whatever Snapshot your game uses.
	using MySnapshot = Netcode.Snapshot;

	// @class Client
	// @desc Performs client-side prediction and server synchronization. 
	public class Client : Netcode.MultiplayerNetworking {
		public const string MASTER_SERVER_IP = "127.0.0.1";
		public const int MASTER_SERVER_PORT = 9001;

		// The rate at which updates are sent to the server. 
		public const float TICK_RATE = 0f;
		// Address of the server. Initially, we talk to the master server. The server address changes to the 
		// game server that the master connects us to.
		public Netcode.ClientAddress m_serverAddr = new Netcode.ClientAddress(MASTER_SERVER_IP, MASTER_SERVER_PORT);
		// The client-side game logic.
		public GameClient m_game;
		// Queue of client commands. 
		private Queue<Netcode.CmdType> m_cmdQueue = new Queue<Netcode.CmdType>();
		// Client snapshots. 
		private Netcode.SnapshotHistory<MySnapshot> m_snapshotHistory = new Netcode.SnapshotHistory<MySnapshot>();
		// The tick function sends client commands at the specified tick rate.
		private Netcode.PeriodicFunction m_tick;
		// Client's current seqno.
		private uint m_seqno = 0;

		private delegate uint NewSeqnoDel();
		NewSeqnoDel m_newSeqno;

		void Start()
		{
			SetMultiplayerGame(m_game);
			RegisterPacket(Netcode.PacketType.SNAPSHOT, ProcessSnapshot);
			InitUdp();

			m_tick = new Netcode.PeriodicFunction(() => { }, 2f);
			m_newSeqno = new NewSeqnoDel(GetSeqno);
		}

		void SnapshotTick()
		{
			// @test 
			GameObject mainPlayer = m_game.GetMainPlayer();
			MySnapshot snapshot = new MySnapshot(m_newSeqno(), m_game.GetServerId(), mainPlayer);

			m_snapshotHistory.PutSnapshot(snapshot);

			SendPacket(m_serverAddr, snapshot);
			// @endtest 
		}

		void Update()
		{
			// Process the packets in the incoming queue.
			while (m_mainWork.Count > 0) {
				Netcode.MainThreadWork work = m_mainWork.Dequeue();
				if (work != null)
					work.Invoke();
			}

			// Send tick to server.
			m_tick.Run();

			// Send the packets in the outgoing queue (the packets that the game requested us to send). 
			Queue<byte[]> packetQueue = m_game.GetPacketQueue();
			foreach (byte[] packet in packetQueue) {
				SendPacket(m_serverAddr, packet);
			}
		}

		// @func ProcessSnapshot
		// @desc If the snapshot was intended for the main player, and the client and 
		// server are in agreement, the Network Event is ignored.
		void ProcessSnapshot(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			MySnapshot snapshot = Netcode.Serializer.Deserialize<MySnapshot>(buf);

			if (snapshot.m_serverId == m_game.GetServerId()) {
				if (!m_snapshotHistory.Reconcile(snapshot)) {
					ClientLog("Client is out of sync with the server -- reconciling");
					m_game.NetEvent(snapshot);
				}
			} else
				m_game.NetEvent(snapshot);
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