using System.Collections.Generic;
using UnityEngine;
using FpsNetcode;

// @class Game
// @desc Contains game logic code that is shared between the client game and the 
// server game. 
public abstract class Game : MonoBehaviour {
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

	public abstract void NetEvent(Netcode.Connect connect);
	public abstract void NetEvent(Netcode.Disconnect disconnect);
	public abstract GameObject NetEvent(Netcode.Snapshot snapshot);
}