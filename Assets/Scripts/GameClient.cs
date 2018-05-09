using System.Collections.Generic;
using UnityEngine;
using FpsNetcode;

namespace FpsClient {
	// @class Game
	// @desc Simulates the game state on the client side. 
	public class GameClient : MonoBehaviour {
		public GameObject playerPrefab;
		public GameObject mainPlayerPrefab;

		// @doc The game objects on the client side are hashed by the server ID. 
		private Dictionary<int, GameObject> m_objects = new Dictionary<int, GameObject>();

		public GameClient() {}

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

		// @func GetEntity
		// @desc Gets the entity from the server ID. 
		public GameObject GetEntity(int serverId)
		{
			return m_objects[serverId];
		}

		// @func GetMainPlayer
		// @desc Gets the game object associated with the main player. 
		public GameObject GetMainPlayer()
		{
			return mainPlayerPrefab;
		}

		// @func KillEntity
		// @desc Removes the game object with server ID from the game. 
		public void KillEntity(int serverId)
		{
			GameObject killedEntity = GetEntity(serverId);
			m_objects.Remove(serverId);
			Destroy(killedEntity);
		}
	}
}