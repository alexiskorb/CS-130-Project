using System.Collections.Generic;
using UnityEngine;
using FpsNetcode;

public class Game : MonoBehaviour {
	protected Dictionary<int, GameObject> m_objects;

	public void Start()
	{
		m_objects = new Dictionary<int, GameObject>();
	}

	// @func GetEntity
	// @desc Gets the entity from the server ID. 
	public GameObject GetEntity(int serverId)
	{
		return m_objects[serverId];
	}

	// @func PutEntity
	// @desc Inserts the entity into the dictionary. If an entity with this server ID
	// already exists, kill it. 
	public void PutEntity(int serverId, GameObject gameObject)
	{
		if (m_objects.ContainsKey(serverId))
			KillEntity(serverId);
		m_objects[serverId] = gameObject;
	}

	// @func KillEntity
	// @desc Removes the game object with server ID from the game. 
	public void KillEntity(int serverId)
	{
		if (m_objects.ContainsKey(serverId)) {
			GameObject killedEntity = m_objects[serverId];
			Destroy(killedEntity);
			m_objects.Remove(serverId);
		}
	}
}

namespace FpsServer {
	// @class Game
	// @desc Simulates the game state on the server side.
	public class GameServer : Game {
		// @doc The game objects on the server side are hashed by their instance IDs. 
		public GameObject spawnPlayerPrefab;

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

		// @func GetSnapshot
		// @desc Gets a snapshot from the given player object.
		public Netcode.PlayerSnapshot GetSnapshotOfPlayer(GameObject playerObject)
		{
			return new Netcode.PlayerSnapshot(0, playerObject.GetInstanceID(), playerObject.GetComponent<Transform>().position);
		}
	}
}

