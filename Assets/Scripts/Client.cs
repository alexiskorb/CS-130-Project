using UnityEngine;
using System.Collections.Generic;

namespace FpsClient {
	// @doc Change MySnapshot to whatever Snapshot your game uses.
	using MySnapshot = Netcode.Snapshot;

	// @class Client
	// @desc Performs client-side prediction and server synchronization. 
	public class Client : Netcode.MultiplayerNetworking {
		public string MasterServerIp = "127.0.0.1";
		public int MasterServerPort = 9001;
		public uint PREDICTION_BUFFER_SIZE = 20;
		public float TICK_RATE = .06f; // The rate in seconds at which updates are sent to the server.
        public float RELIABLE_TICK_RATE = 1.0f;

        // Address of the server. Initially, we talk to the master server. The server address changes to the 
        // game server that the master connects us to.
        public Netcode.ClientAddress m_lobbyServerAddr;

        // The client-side game logic.
        public GameClient m_game;
		// Client snapshots. 
		private Netcode.SnapshotHistory<MySnapshot> m_snapshotHistory;
		// The tick function sends client commands at the specified tick rate.
		private Netcode.PeriodicFunction m_tick;
        private Netcode.PeriodicFunction m_reliablePacketTick;

        public Netcode.PeriodicFunction Tick
        {
            get { return m_tick;  }
            set { m_tick = value; }
        }
		// Client's current seqno.
		private uint m_seqno = 0;
		public delegate uint NewSeqnoDel();
		// Call this delegate to increment the sequence number.
		public NewSeqnoDel m_newSeqno;
		// Last received hash ("password") from the server.
		private int m_serverHash = 0;
		// Last received server sequence number.
		private uint m_serverSeqno = 0;

		// @func Start
		// @desc Initialize the networking and snapshot history. At first, don't send updates or increment 
		// seqnos.
		void Start()
		{
            RegisterPacket(Netcode.PacketType.SNAPSHOT, ProcessSnapshot);
			RegisterPacket(Netcode.PacketType.BULLET_SNAPSHOT, ProcessBulletSnapshot);
            RegisterPacket(Netcode.PacketType.PLAYER_SNAPSHOT, ProcessPlayerSnapshot);
			InitNetworking(m_game, MasterServerIp, MasterServerPort);

			m_tick = new Netcode.PeriodicFunction(() => { }, 0f);
            m_reliablePacketTick = new Netcode.PeriodicFunction(SendReliablePackets, RELIABLE_TICK_RATE);
            m_newSeqno = new NewSeqnoDel(GetSeqno);
			m_snapshotHistory = new Netcode.SnapshotHistory<MySnapshot>(PREDICTION_BUFFER_SIZE);
		}

		// @func BeginSnapshots
		// @desc Tells the client to start sending snapshots to the server.
		public void BeginSnapshots()
		{
			Tick = new Netcode.PeriodicFunction(SnapshotTick, TICK_RATE);
			m_newSeqno = NewSeqno;
		}

		// @func StopSnapshots
		// @desc Tells the client to stop sending snapshots. Used when the game client 
		// transitions back to the main screen. 
		public void StopSnapshots()
		{
			Tick = new Netcode.PeriodicFunction(()=>{}, 0f);
			m_seqno = 0;
		}

        public void OnEnable()
        {
            DontDestroyOnLoad(this.gameObject);
        }

		// @func SnapshotTick
		// @desc This tick function sends player input and snapshots.
        public void SnapshotTick()
		{
			uint newSeqno = m_newSeqno();

			GameObject mainPlayer = m_game.GetMainPlayer();
			MySnapshot state = new MySnapshot(newSeqno, m_game.mainPlayerServerId, m_serverHash, mainPlayer);
			m_snapshotHistory.PutSnapshot(state);
			SendPacket(m_lobbyServerAddr, state);

			Netcode.InputBit inputBits = m_game.GetInput();
			Netcode.PlayerInput playerInput = new Netcode.PlayerInput(newSeqno, m_game.mainPlayerServerId, inputBits);

			SendPacket(m_lobbyServerAddr, playerInput);
		}

		// @func Update
		// @desc Process packets in the incoming queue, send tick to server, and send outgoing packets.
		void Update()
		{
			ProcessPacketsInQueue();

			m_tick.Run();

			Queue<byte[]> packetQueue = m_game.GetPacketQueue();
			foreach (byte[] packet in packetQueue) {
				SendPacket(m_lobbyServerAddr, packet);
			}
            var packetsForClient = m_game.GetPacketsForClient();
            foreach (Netcode.PacketForClient packet in packetsForClient)
            {
                SendPacket(packet.m_clientAddr, packet.m_packet);
            }

            m_reliablePacketTick.Run();
        }

		void ProcessBulletSnapshot(Netcode.ClientAddress clientAddress, byte[] buf)
		{
			Netcode.BulletSnapshot bulletSnapshot = Netcode.Serializer.Deserialize<Netcode.BulletSnapshot>(buf);
			m_game.NetEvent(bulletSnapshot);
		}

        void ProcessPlayerSnapshot(Netcode.ClientAddress clientAddress, byte[] buf)
        {
            Netcode.PlayerSnapshot playerSnapshot = Netcode.Serializer.Deserialize<Netcode.PlayerSnapshot>(buf);
            m_game.NetEvent(playerSnapshot);
        }

        // @func ProcessSnapshot
        // @desc If the snapshot was intended for the main player, and the client and 
        // server are in agreement, the Network Event is ignored.
        void ProcessSnapshot(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			MySnapshot snapshot = Netcode.Serializer.Deserialize<MySnapshot>(buf);

			if (snapshot.m_seqno < m_serverSeqno) {
				Debug.Log("Snapshot from server received out of order.");
				return;
			}

			if (snapshot.m_serverId == m_game.mainPlayerServerId) {
				if (!m_snapshotHistory.Reconcile(snapshot)) {
					Debug.Log("Client is out of sync with the server -- reconciling");
					m_serverSeqno = snapshot.m_seqno;
					m_game.NetEvent(snapshot);
				}
			} else
				m_game.NetEvent(snapshot);
		}
        void SendReliablePackets()
        {
            var reliableQueue = m_game.GetReliablePackets();
            foreach (var packet in reliableQueue.Values)
            {
                SendPacket(packet.m_clientAddr, packet.m_packet);
            }
        }

        // @func NewSeqno
        // @desc Returns the current seqno and increments it. 
        public uint NewSeqno()
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

		// @func ShouldDiscard
		// @desc Drop UDP packets that aren't from the server. 
		public override bool ShouldDiscard(Netcode.ClientAddress clientAddr, Netcode.Packet header)
		{
			return !(m_lobbyServerAddr.m_ipAddress == clientAddr.m_ipAddress &&
				m_lobbyServerAddr.m_port == clientAddr.m_port);
		}
	}
}
