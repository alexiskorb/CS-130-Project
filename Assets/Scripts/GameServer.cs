using System.Collections.Generic;
using UnityEngine;
using FpsNetcode;

namespace FpsServer {
	// @class Game
	// @desc Simulates the game state on the server side.
	public class GameServer : MonoBehaviour {
		public GameObject spawnPlayerPrefab;

		// @doc The game objects on the server side are hashed by their instance IDs. 
		private Dictionary<int, GameObject> m_objects = new Dictionary<int, GameObject>();

		public GameServer() {}

		// @func SpawnPlayer
		// @desc Spawns a new player and returns its initial snapshot.
		public Netcode.PlayerSnapshot SpawnPlayer()
		{
			GameObject gameObject = Instantiate(spawnPlayerPrefab);
			Netcode.PlayerSnapshot snapshot = new Netcode.PlayerSnapshot(0, gameObject.GetInstanceID(), new Vector3(0, 1, 0));
			Netcode.ApplySnapshot(ref gameObject, snapshot);
			m_objects[gameObject.GetInstanceID()] = gameObject;
			return snapshot;
		}

		// @func PutSnapshot
		// @desc Inserts the given snapshot.
		public GameObject PutSnapshot(Netcode.PlayerSnapshot snapshot)
		{
			GameObject gameObject = GetEntity(snapshot.m_serverId);
			Netcode.ApplySnapshot(ref gameObject, snapshot);
			return gameObject;
		}

		// @func GetEntity
		// @desc Gets the entity from the server ID. 
		public GameObject GetEntity(int serverId)
		{
			return m_objects[serverId];
		}

		// @func KillEntity
		// @desc Removes the game object with server ID from the game. 
		public void KillEntity(int serverId)
		{
			GameObject killedEntity = m_objects[serverId];
			m_objects.Remove(serverId);
			Destroy(killedEntity);
		}

		// @func GetSnapshot
		// @desc Gets a snapshot from the given player object.
		public Netcode.PlayerSnapshot GetSnapshotOfPlayer(GameObject playerObject)
		{
			return new Netcode.PlayerSnapshot(0, playerObject.GetInstanceID(), playerObject.GetComponent<Transform>().position);
		}
	}
}

