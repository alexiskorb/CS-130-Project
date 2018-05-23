using UnityEngine;
using System.Collections.Generic;

// @TODO Commands, reliable connect/disconnect

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
		private const int SERVER_PORT = 9001;

		// The server-side game manager.
		public GameServer m_game;
		// Snapshots sent by all clients. 
		private ClientHistoryMapping m_clients = new ClientHistoryMapping();
		// Mapping from address to server ID. 
		private ServerIdMapping m_serverIds = new ServerIdMapping();
        public ClientHistoryMapping Clients
        {
            get { return m_clients; }
        }
        public ServerIdMapping ServerIds
        {
            get { return m_serverIds; }
        }

        void Start()
		{
			SetMultiplayerGame(m_game);
			RegisterPacket(Netcode.PacketType.SNAPSHOT, ProcessSnapshot);
			InitUdp(SERVER_PORT);
            Debug.Log("Opened Server");
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
				Netcode.SnapshotHistory<MySnapshot> history = client.Value;
				Netcode.ClientAddress clientAddr = client.Key;

				history.IncrTimeSinceLastAck(Time.deltaTime);

				if (history.GetTimeSinceLastAck() >= Netcode.SnapshotHistory<MySnapshot>.CLIENT_TIMEOUT) {
					ServerLog("Disconnecting Client. " + clientAddr.Print());
					break;
				}

				MySnapshot snapshot = history.GetMostRecentSnapshot();
				BroadcastPacket(snapshot);
			}

            var packetQueue = m_game.GetPacketQueue();
            foreach (var packet in packetQueue)
                BroadcastPacket(packet);

            var packetsForClient = m_game.GetPacketsForClient();
            foreach (var packet in packetsForClient)
                SendPacket(packet.m_clientAddr, packet.m_packet);
		}

        public void OnEnable()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        // @TODO
        public void ProcessClientCmd()
		{
		}

		public void ProcessSnapshot(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			MySnapshot snapshot = Netcode.Serializer.Deserialize<MySnapshot>(buf);
			Netcode.SnapshotHistory<MySnapshot> history = m_clients[clientAddr];

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
			byte[] buf = Netcode.Serializer.Serialize(packet);
			foreach (var clientAddr in m_clients.Keys) {
				SendPacket(clientAddr, buf);
			}
		}

		void OnDestroy()
		{
		}

		public void ServerLog(string message)
		{
			Debug.Log("<Server> " + message);
		}

		public override bool ShouldDiscard(Netcode.ClientAddress clientAddr, Netcode.Packet header)
		{
			return false;
		}
	}

}