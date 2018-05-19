using UnityEngine;

namespace FpsClient {
	// @class Game
	// @desc Simulates the game state on the client side. 
	public class GameClient : Game {
		public GameObject playerPrefab;
		public GameObject m_mainPlayer;

		// The server ID of the main player.
		private int m_serverId = 0;

		public void Update() {}

		// @func SpawnPlayer
		// @desc Spawns a new player with the given snapshot. 
		private GameObject SpawnPlayer(Netcode.Snapshot snapshot)
		{
			GameObject gameObject = Instantiate(playerPrefab);
			snapshot.Apply(ref gameObject);
			m_objects[snapshot.m_serverId] = gameObject;
			return gameObject;
		}

		// @func GetMainPlayer
		// @desc Gets the game object associated with the main player. 
		public GameObject GetMainPlayer()
		{
			return m_mainPlayer;
		}

		// @func GetServerId
		// @desc Gets the main player's server ID.
		public int GetServerId()
		{
			return m_serverId;
		}

		// @func SetServerId
		// @desc Set the server ID to the new one.
		public void SetServerId(int serverId)
		{
			m_serverId = serverId;
		}

		// @func NetEvent.Snapshot
		// @desc If this is called, the game has received a snapshot. 
		public override void NetEvent(Netcode.Snapshot snapshot)
		{
			GameObject gameObject;

			if (!m_objects.ContainsKey(snapshot.m_serverId)) {
				gameObject = SpawnPlayer(snapshot);
			} else {
				gameObject = GetEntity(snapshot.m_serverId);
				snapshot.Apply(ref gameObject);
			}
		}

		public override void NetEvent(Netcode.ClientAddress clientAddr, Netcode.PacketType type, byte[] buf)
		{
			switch (type) {
				case Netcode.PacketType.REFRESH_LOBBY_LIST:
					// @CHRIS
					break;
				case Netcode.PacketType.CREATE_LOBBY:
					// @CHRIS
					break;
				case Netcode.PacketType.JOIN_LOBBY:
					// @CHRIS
					break;
				case Netcode.PacketType.START_GAME:
					break;
			}
		}
	}
}