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

        public uint PREDICTION_BUFFER_SIZE = 40;
		// Rate at which updates are sent to the server.
		public float TICK_RATE = 0.01667f;
		// Enables performance logging.
		public bool m_enablePerformanceLog = true;
        // Rate at which reliable packets are repeatedly sent.
        public float RELIABLE_TICK_RATE = 1.0f;
        // Rate at which heartbeat packets are repeatedly sent.
        public float HEART_BEAT_RATE = 30.0f;


        // The server-side game manager.
        public GameServer m_game;
		// Snapshots sent by all clients. 
		public ClientHistoryMapping m_clients = new ClientHistoryMapping();
		// Mapping from address to server ID. 
		public ServerIdMapping m_serverIds = new ServerIdMapping();
		// The function that gets called every tick, where tick is 
		// the rate at which updates are sent to the server.
		private Netcode.PeriodicFunction m_tick;
        // The tick function sends reliable packets at the specified tick rate.
        private Netcode.PeriodicFunction m_reliablePacketTick;
        // The tick function sends heartbeat messages to masterserver to remain connections.
        private Netcode.PeriodicFunction m_heartBeatTick;
        // The time spent processing packets in one frame. 
        private System.TimeSpan timeProcessingPackets;
		// m_clients getter.
		public ClientHistoryMapping Clients {
			get { return m_clients; }
		}
		// m_serverIds getter.
		public ServerIdMapping ServerIds {
			get { return m_serverIds; }
		}

		// @func Start
		// @desc Initialize networking.
		void Start()
		{
			RegisterPacket(Netcode.PacketType.PLAYER_INPUT, ProcessPlayerInput);
			RegisterPacket(Netcode.PacketType.SNAPSHOT, ProcessSnapshot);
            InitNetworking(m_game, MasterServerIp, MasterServerPort, SERVER_PORT);

            m_tick = new Netcode.PeriodicFunction(MainServerLoop, TICK_RATE);
            m_reliablePacketTick = new Netcode.PeriodicFunction(SendReliablePackets, RELIABLE_TICK_RATE);
            m_heartBeatTick = new Netcode.PeriodicFunction(SendHeartBeatsToMaster, HEART_BEAT_RATE);
        }
        // @func Update
        // @desc Every update we do all the work in the work queue, then propagate updates to all connected clients.
        void Update()
		{
			if (m_enablePerformanceLog) {
				var time = (timeProcessingPackets.Milliseconds / (Time.deltaTime * 10));
				if (time > 0f)
					Debug.Log("% time spent processing packets: " + time);
				var watch = System.Diagnostics.Stopwatch.StartNew();
				ProcessPacketsInQueue();
				watch.Stop();
				timeProcessingPackets = watch.Elapsed;
			} else {
				ProcessPacketsInQueue();
			}

			m_tick.Run();
		}

		// @func MainServerLoop
		// @desc Runs every tick. Sends snapshots, game state, and packets requested by the client.
		private void MainServerLoop()
		{
			foreach (var client in m_clients) {
				Netcode.SnapshotHistory<MySnapshot> history = client.Value;
				Netcode.ClientAddress clientAddr = client.Key;

				history.IncrTimeSinceLastAck(Time.deltaTime);

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
            //If there are any reliable packets that need to be retransmitted, run.
            m_reliablePacketTick.Run();
            //Send heartbeat messages to masterserver if necessary.
            m_heartBeatTick.Run();

            SendState<Bullet, Netcode.BulletSnapshot>();
			SendState<NetworkedPlayer, Netcode.PlayerSnapshot>();
		}

		// @func SendState
		// @desc Send the state of game object G with snapshot packet T. 
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

		// @func ProcessPlayerInput
		// @desc Inform the game that a PlayerInput netevent has occurred.
		public void ProcessPlayerInput(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			Netcode.PlayerInput playerInput = Netcode.Serializer.Deserialize<Netcode.PlayerInput>(buf);
			m_game.NetEvent(playerInput);
		}

		// @func ProcessSnapshot
		// @desc Process snapshots, checking the seqno to discard old snapshots.
		public void ProcessSnapshot(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			if (!m_clients.ContainsKey(clientAddr))
				return;

			MySnapshot snapshot = Netcode.Serializer.Deserialize<MySnapshot>(buf);
			Netcode.SnapshotHistory<MySnapshot> history = m_clients[clientAddr];

			if (snapshot.m_seqno < history.GetSeqno()) {
				Debug.Log("Snapshot with seqno " + snapshot.m_seqno + " was received out of order.");
				return;
			}

			m_clients[clientAddr].PutSnapshot(snapshot);
			m_game.NetEvent(snapshot);
		}

		// @func BroadcastPacket
		// @desc Takes a packet structure, serializes, and broadcasts it to all connected clients.
		void BroadcastPacket<T>(T packet)
		{
			byte[] buf = Netcode.Serializer.Serialize(packet);
			foreach (var clientAddr in m_clients.Keys)
				SendPacket(clientAddr, buf);
		}

		// @func BroadcastPacket
		// @desc Broadcasts the packet to all connected 
		void BroadcastPacket(byte[] buf)
		{
			foreach (var clientAddr in m_clients.Keys) {
				SendPacket(clientAddr, buf);
			}
		}
        // @func SendReliablePackets
        // @desc Called every RELIABLE_TICK_RATE to send any packets that require reliable transmission.
        void SendReliablePackets()
        {
            var reliableQueue = m_game.GetReliablePackets();
            foreach (var packet in reliableQueue.Values)
            {
                SendPacket(packet.m_clientAddr, packet.m_packet);
            }
        }
        // @func SendHeartBeatsToMaster
        // @desc Every HEART_BEAT_RATE, send a packet to the masterserver so you can still establish connection with the masterserver.
        // This is used since routers can block any incoming packets from ports that a host hasn't communicated with in a while.
        void SendHeartBeatsToMaster()
        {
            string commandName = "lobup";
            byte[] buf = System.Text.Encoding.UTF8.GetBytes(commandName);
            SendPacket(MasterServer, buf);
        }

        // @func ShouldDiscard
        // @desc If we're the server, accept packets from anyone. When the master server transfers control
        // to the game server, we should probably 
        public override bool ShouldDiscard(Netcode.ClientAddress clientAddr, Netcode.Packet header)
		{
			return false;
		}

		// @func RemoveClient
		// @desc Removes all client data from the server.
		public void RemoveClient(Netcode.ClientAddress clientAddr)
		{
			List<Netcode.ClientAddress> clients = new List<Netcode.ClientAddress>(m_clients.Keys);
			foreach (Netcode.ClientAddress address in clients) {
				if (address.m_ipAddress == clientAddr.m_ipAddress && address.m_port == clientAddr.m_port) {
					m_clients.Remove(address);
					m_serverIds.Remove(address);
				}
			}
		}
	}

}
