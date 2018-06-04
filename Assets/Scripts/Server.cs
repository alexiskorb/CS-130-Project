using UnityEngine;
using System.Collections.Generic;

namespace FpsServer {
	// @alias Change MySnapshot to whatever Snapshot your game uses.
	using MySnapshot = Netcode.Snapshot;
	// @alias The mapping between client address and client history
	using ClientHistoryMapping = Dictionary<Netcode.ClientAddress, Netcode.SnapshotHistory<Netcode.Snapshot>>;
	// @alias The mapping between client address and server ID.
	using ServerIdMapping = Dictionary<Netcode.ClientAddress, int>;

	// @class Server
	// @desc The authoritative server.
	public class Server : Netcode.MultiplayerNetworking {
        public string SERVER_IP = "127.0.0.1";
        public int SERVER_PORT = 9001;

        public string MasterServerIp;
        public int MasterServerPort;

		public uint PREDICTION_BUFFER_SIZE = 20;
		public float TICK_RATE = 0f;
		public bool m_enablePerformanceLog = true;
        public float RELIABLE_TICK_RATE = 1.0f;

		// The server-side game manager.
		public GameServer m_game;
		// Snapshots sent by all clients. 
		public ClientHistoryMapping m_clients = new ClientHistoryMapping();
		// Mapping from address to server ID. 
		public ServerIdMapping m_serverIds = new ServerIdMapping();

        public ClientHistoryMapping Clients
        {
            get { return m_clients; }
        }
        public ServerIdMapping ServerIds
        {
            get { return m_serverIds; }
        }

		private Netcode.PeriodicFunction m_tick;
        private Netcode.PeriodicFunction m_reliablePacketTick;

		private System.TimeSpan timeProcessingPackets;

        void Start()
        {
            RegisterPacket(Netcode.PacketType.PLAYER_INPUT, ProcessPlayerInput);
            RegisterPacket(Netcode.PacketType.SNAPSHOT, ProcessSnapshot);
            InitNetworking(m_game, MasterServerIp, MasterServerPort, SERVER_PORT);

            m_tick = new Netcode.PeriodicFunction(MainServerLoop, TICK_RATE);
            m_reliablePacketTick = new Netcode.PeriodicFunction(SendReliablePackets, RELIABLE_TICK_RATE);
        }
		// @func Update
		// @desc Every update we do all the work in the work queue, then propagate updates to all connected clients.
		void Update()
		{
			if (m_enablePerformanceLog) {
				Debug.Log("% time spent processing packets: " + (timeProcessingPackets.Milliseconds / (Time.deltaTime * 10)));
				var watch = System.Diagnostics.Stopwatch.StartNew();
				ProcessPacketsInQueue();
				watch.Stop();
				timeProcessingPackets = watch.Elapsed;
			} else {
				ProcessPacketsInQueue();
			}

			m_tick.Run();
		}

		private void MainServerLoop()
		{
			foreach (var client in m_clients) {
				Netcode.SnapshotHistory<MySnapshot> history = client.Value;
				Netcode.ClientAddress clientAddr = client.Key;

				history.IncrTimeSinceLastAck(Time.deltaTime);

				if (history.GetTimeSinceLastAck() >= Netcode.SnapshotHistory<MySnapshot>.CLIENT_TIMEOUT) {
					Debug.Log("Disconnecting Client. " + clientAddr.Print());
					break;
				}

				MySnapshot snapshot = history.GetMostRecentSnapshot();
				BroadcastPacket(snapshot);
			}

			var packetQueue = m_game.GetPacketQueue();
			foreach (var packet in packetQueue)
				BroadcastPacket(packet);

			var packetsForClient = m_game.GetPacketsForClient();
            foreach (Netcode.PacketForClient packet in packetsForClient)
            {
                SendPacket(packet.m_clientAddr, packet.m_packet);
            }

            //m_reliablePacketTick.Run();

            SendState<Bullet, Netcode.BulletSnapshot>();
			SendState<NetworkedPlayer, Netcode.PlayerSnapshot>();
		}

		private void SendState<G, T>() where G : MonoBehaviour where T : Netcode.ISnapshot<T>, new()
		{
			var gameObjects = m_game.FindNetworkObjects<G>();
			foreach (var gameObject in gameObjects) {
				T playerState = new T();
				playerState.Create(0, gameObject.GetInstanceID(), gameObject);
				BroadcastPacket(playerState);
			}
		}

		public void OnEnable()
        {
            DontDestroyOnLoad(this.gameObject);
        }

		public void ProcessPlayerInput(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			Netcode.PlayerInput playerInput = Netcode.Serializer.Deserialize<Netcode.PlayerInput>(buf);
			m_game.NetEvent(playerInput);
		}

		public void ProcessSnapshot(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			MySnapshot snapshot = Netcode.Serializer.Deserialize<MySnapshot>(buf);
			Netcode.SnapshotHistory<MySnapshot> history = m_clients[clientAddr];

			// Check the seqno, discarding old snapshots.
			if (snapshot.m_seqno < history.GetSeqno()) {
				Debug.Log("Snapshot with seqno " + snapshot.m_seqno + " was received out of order.");
				return;
			}

			m_clients[clientAddr].PutSnapshot(snapshot);
			m_game.NetEvent(snapshot);
		}

		// @func BroadcastPacket
		// @desc Broadcasts the packet to all connected clients.
		void BroadcastPacket<T>(T packet)
		{
			byte[] buf = Netcode.Serializer.Serialize(packet);
			foreach (var clientAddr in m_clients.Keys) {
				SendPacket(clientAddr, buf);
			}
		}
        void SendReliablePackets()
        {
            var reliableQueue = m_game.GetReliablePackets();
            foreach (var packet in reliableQueue.Values)
            {
                SendPacket(packet.m_clientAddr, packet.m_packet);
            }
        }

		public override bool ShouldDiscard(Netcode.ClientAddress clientAddr, Netcode.Packet header)
		{
			return false;
		}
	}

}
