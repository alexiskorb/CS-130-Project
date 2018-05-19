using UnityEngine;
using System.Collections.Generic;

namespace FpsServer {
	// @class Game
	// @desc Simulates the game state on the server side.
	// The game objects on the server side are hashed by their Unity instance IDs. 
	public class GameServer : Game {
		public GameObject spawnPlayerPrefab;

		public void Startup()
		{
		}

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
		public override void NetEvent(Netcode.Snapshot snapshot)
		{
			GameObject gameObject = GetEntity(snapshot.m_serverId);
			snapshot.Apply(ref gameObject);
		}

		public override void NetEvent(Netcode.ClientAddress clientAddr, Netcode.PacketType packetType, byte[] buf)
		{
		}
	}
}

