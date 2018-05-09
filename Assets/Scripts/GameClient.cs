using UnityEngine;
using FpsNetcode;

namespace FpsClient {
	// @class Game
	// @desc Simulates the game state on the client side. 
	public class GameClient : Game {
		public GameObject playerPrefab;
		public GameObject m_mainPlayer;

		// The server ID of the main player.
		public int m_serverId = 0;

		// @func SpawnPlayer
		// @desc Spawns a new player with the given snapshot. 
		private GameObject SpawnPlayer(Netcode.PlayerSnapshot snapshot)
		{
			GameObject gameObject = Instantiate(playerPrefab);
			Netcode.ApplySnapshot(ref gameObject, snapshot);
			m_objects[snapshot.m_serverId] = gameObject;
			return gameObject;
		}

		// @func PutSnapshot
		// @desc Inserts the given snapshot. If the Entity Manager doesn't already have 
		// snapshots recorded for the given server ID, spawn a new entity.
		public void PutSnapshot(Netcode.PlayerSnapshot snapshot)
		{
			if (!m_objects.ContainsKey(snapshot.m_serverId)) {
				SpawnPlayer(snapshot);
			} else {
				GameObject entity = GetEntity(snapshot.m_serverId);
				Netcode.ApplySnapshot(ref entity, snapshot);
			}
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

		// @doc NetEvents 

		// @func NetEvent (Connect)
		// @desc 
		public void NetEvent(Netcode.Connect connect)
		{
			SetServerId(connect.m_serverId);
			PutEntity(GetServerId(), m_mainPlayer);
		}

		//public Netcode.PlayerSnapshot GetMainPlayerSnapshot()
		//{
		//}
	}
}