using System.Collections.Generic;
using UnityEngine;
using FpsNetcode;

// @class MultiplayerGame
// @desc Games should implement this interface in order to be alerted to network events. 
public abstract class MultiplayerGame : MonoBehaviour {
	public abstract GameObject NetEvent(Netcode.Connect connect);
	public abstract GameObject NetEvent(Netcode.Disconnect disconnect);
	public abstract GameObject NetEvent(Netcode.Snapshot snapshot);
}

// @class Game
// @desc Contains game logic code that is shared between the client game and the 
// server game. 
public abstract class Game : MultiplayerGame {
	protected Dictionary<int, GameObject> m_objects;

	public void Start()
	{
		m_objects = new Dictionary<int, GameObject>();
	}

	// @func GetEntity
	// @desc Gets the entity from the server ID. 
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