using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class PacketTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
        string[] playerlist = { "abc", "def" };
        string CurrentLobby = "heljhibiubilo";
        string MainPlayerName = "bobwewfewewffwefe";
        Netcode.CreateLobby packet = new Netcode.CreateLobby(CurrentLobby, MainPlayerName);
        byte[] joinBuf = Netcode.Serializer.Serialize(packet);
        Netcode.CreateLobby des = Netcode.Serializer.Deserialize<Netcode.CreateLobby>(joinBuf);
        Debug.Log(des.m_type);
        //Debug.Log(des.m_listOfPlayers[1]);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
