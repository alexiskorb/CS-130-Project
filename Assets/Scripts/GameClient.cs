using UnityEngine;
using FpsNetcode;

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
			Netcode.Snapshot.Apply(ref gameObject, snapshot);
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
		public override GameObject NetEvent(Netcode.Snapshot snapshot)
		{
			GameObject gameObject;

			if (!m_objects.ContainsKey(snapshot.m_serverId)) {
				gameObject = SpawnPlayer(snapshot);
			} else {
				gameObject = GetEntity(snapshot.m_serverId);
				Netcode.Snapshot.Apply(ref gameObject, snapshot);
			}

			return gameObject;
		}

		// @func NetEvent.Connect
		// @desc Do something when we get a connect ack... Say, play some music, show a load screen, or join a chat room. 
		// Here, we just jump right into the game.
		public override void NetEvent(Netcode.Connect connect)
		{
			SetServerId(connect.m_serverId);
			PutEntity(GetServerId(), m_mainPlayer);
		}

		// @func NetEvent.Disconnect
		// @desc When we get a disconnect packet, the game should probably go to the main menu
		// scene, or send your mom an email, or whatever -- again, it's up to the game!. 
		public override void NetEvent(Netcode.Disconnect disconnect)
		{
			if (m_serverId == disconnect.m_serverId) {
				// End game, return to lobby, whatever
			} else {
				KillEntity(disconnect.m_serverId);
			}
		}
	}
}