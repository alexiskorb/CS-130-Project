using UnityEngine;
using FpsNetcode;

namespace FpsServer {
	// @class Game
	// @desc Simulates the game state on the server side.
	// The game objects on the server side are hashed by their Unity instance IDs. 
	public class GameServer : Game {
		public GameObject spawnPlayerPrefab;

		public void Update() {}

		// @func SpawnPlayer
		// @desc Spawns a new player and returns it
		public GameObject SpawnPlayer()
		{
			GameObject gameObject = Instantiate(spawnPlayerPrefab);
			gameObject.transform.position = new Vector3(0, 1, 0);
			PutEntity(gameObject.GetInstanceID(), gameObject);
			return gameObject;
		}

		// @func PutSnapshot
		// @desc The server received a snapshot. 
		public override GameObject NetEvent(Netcode.Snapshot snapshot)
		{
			GameObject gameObject = GetEntity(snapshot.m_serverId);
			snapshot.Apply(ref gameObject);
			return gameObject;
		}

		public override GameObject NetEvent(Netcode.Connect connect)
		{
			return SpawnPlayer();
		}

		// @func NetEvent.Disconnect
		// @desc In this particular game, when we get a disconnect, we just kill the entity.
		public override GameObject NetEvent(Netcode.Disconnect disconnect)
		{
			KillEntity(disconnect.m_serverId);
			return null;
		}
	}
}

