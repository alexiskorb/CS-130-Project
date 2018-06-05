//using UnityEngine;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine.SceneManagement;
//using System;
//using System.Collections;
//namespace FpsServer
//{
//    public class MasterServer : Netcode.MultiplayerNetworking
//    {
//        private const int MASTER_SERVER_PORT = 8484;

//        Dictionary<string, ArrayList> ServerList = new Dictionary<string, ArrayList>();
//        Dictionary<string, ArrayList> OpenLobby = new Dictionary<string, ArrayList>();
//        Dictionary<string, string> LobbyPort = new Dictionary<string, string>();
//        Dictionary<string, ArrayList> LobbyInfo = new Dictionary<string, ArrayList>();
//        Dictionary<string, ArrayList> PlayerList = new Dictionary<string, ArrayList>();
//        Dictionary<string, string> CurrentGame = new Dictionary<string, string>();


//        // Use this for initialization
//        void Start()
//        {

//        }

//        // Update is called once per frame
//        void Update()
//        {

//        }

//        public override bool ShouldDiscard(Netcode.ClientAddress clientAddr, Netcode.Packet header)
//        {
//            return false;
//        }
//    }
//}
