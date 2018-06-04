using System.Collections.Generic;
using UnityEngine;

// @class Game
// @desc Contains game logic code that is shared between the client game and the 
// server game. 
public abstract class Game : Netcode.IMultiplayerGame {
	// GameServer hashes GameObjects by the Unity instance ID; GameClient hashes them by 
	// the server's Unity instance IDs.
	protected Dictionary<int, GameObject> m_objects = new Dictionary<int, GameObject>();

	// @func GetEntity
	// @desc Gets the entity from the server ID. 
	// Always check for null. It's possible that the entity was destroyed and 
	// this is an old packet.
	public GameObject GetEntity(int serverId)
	{
		if (m_objects.ContainsKey(serverId)) {
			return m_objects[serverId];
		} else {
			return null;
		}
	}

	// @func PutEntity
	// @desc Inserts the entity into the dictionary. If an entity with this server ID
	// already exists, kill it. 
	public GameObject PutEntity(int serverId, GameObject gameObject)
	{
		if (m_objects.ContainsKey(serverId))
			KillEntity(serverId);
		m_objects[serverId] = gameObject;
		return gameObject;
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