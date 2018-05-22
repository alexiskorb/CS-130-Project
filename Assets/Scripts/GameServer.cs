using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace FpsServer {
	// @class Game
	// @desc Simulates the game state on the server side.
	// The game objects on the server side are hashed by their Unity instance IDs. 
	public class GameServer : Game {
		public GameObject spawnPlayerPrefab;
        private Dictionary<string, List<string>> m_listOfMatches = new Dictionary<string, List<string>>();
        private Dictionary<string, Netcode.ClientAddress> m_clientAddresses= new Dictionary<string, Netcode.ClientAddress>();

        public void Startup()
		{
		}

		public void Update() {}

		// @func PutSnapshot
		// @desc The server received a snapshot. 
		public override void NetEvent(Netcode.Snapshot snapshot)
		{
			GameObject gameObject = GetEntity(snapshot.m_serverId);
			snapshot.Apply(ref gameObject);
		}

        //*******************************
        //TODO: The functions below will likely be called by the Master server eventually.


        // @func NetEvent.PacketType 
        // @desc If this is called, the game has received a packet, that is not a snapshot
        // The function analyzes the packet type and responds accordingly.
        public override void NetEvent(Netcode.ClientAddress clientAddr, Netcode.PacketType type, byte[] buf)
		{
            switch (type)
            {
                case Netcode.PacketType.REFRESH_LOBBY_LIST:
                    ProcessRefreshLobbyList(clientAddr);
                    break;
                case Netcode.PacketType.CREATE_LOBBY:
                    ProcessCreateLobby(clientAddr, buf);
                    break;
                case Netcode.PacketType.JOIN_LOBBY:
                    ProcessJoinLobby(clientAddr, buf);
                    break;
                case Netcode.PacketType.START_GAME:
                    ProcessStartGame(buf);
                    break;
            }
        }

        // @func RefreshLobbyList
        // @desc A client asked for LobbyList. Sends the packet to the client.
        public void ProcessRefreshLobbyList(Netcode.ClientAddress clientAddr)
        {
            string[] lobbyList = m_listOfMatches.Keys.ToArray();
            Netcode.RefreshLobbyList packet = new Netcode.RefreshLobbyList(lobbyList);
            QueuePacket(clientAddr, packet);
        }
        // @func ProcessCreateLobby
        // @desc Client wants to create a lobby. Add it to the list of lobbies, then reply back 
        // to the client with a JoinLobby confirmation packet.
        public void ProcessCreateLobby(Netcode.ClientAddress clientAddr, byte[] buf)
        {
            Netcode.CreateLobby lobby = Netcode.Serializer.Deserialize<Netcode.CreateLobby>(buf);
            m_listOfMatches.Add(lobby.m_lobbyName, new List<string> { lobby.m_hostPlayerName });
            ProcessJoinLobby(clientAddr, buf);
        }
        // @func ProcessJoinLobby
        // @desc A client requests to join a lobby. Add the player to the list, and store player string 
        // and connection info. Send a packet to all players in that lobby with an updated list of players
        // in the lobby.
        public void ProcessJoinLobby(Netcode.ClientAddress clientAddr, byte[] buf)
        {
            Netcode.JoinLobby lobby = Netcode.Serializer.Deserialize<Netcode.JoinLobby>(buf);
            string lobbyName = lobby.m_lobbyName;

            m_listOfMatches[lobbyName].Add(lobby.m_playerName);
            m_clientAddresses[lobby.m_playerName] = clientAddr;

            Netcode.JoinLobby packet = new Netcode.JoinLobby(m_listOfMatches[lobbyName].ToArray(), lobbyName, "");
            foreach (string player in m_listOfMatches[lobbyName])
            {
                QueuePacket(m_clientAddresses[player], packet);
            } 
        }
        // @func ProcessStartGame
        // @desc A client requests to begin a match from an existing lobby. Find/Create a server to handle the match,
        // and get its IP & port. Give the list of players and their ClientAddress off to the new match server.
        public void ProcessStartGame(byte[] buf)
        {
            Netcode.StartGame game = Netcode.Serializer.Deserialize<Netcode.StartGame>(buf);
            string matchName = game.m_matchName;
            List<string> matchPlayers = m_listOfMatches[matchName];
            //TODO: Find IP and Port of server hosting
            string hostIP;
            int hostPort;
            //Send list of names and IP to new server
            RunMatch(matchName, matchPlayers);
        }


        //********************************
        //This will be called by the server running the instance of the match

        // @func RunMatch
        // @desc The match server will initialize a gameobject for each client, and send every client in the match
        // their unique ServerID, and the hostIP and hostPort to start communicating with.
        public void RunMatch(string matchName, List<string> players)
        {
            string hostIP;
            int hostPort;
            hostIP = "127.0.0.1";
            hostPort = 9001;
            foreach (string client in players)
            {
                Netcode.ClientAddress clientAddress= m_clientAddresses[client];
                int clientId = SpawnPlayer();
                Netcode.StartGame game = new Netcode.StartGame(matchName, clientId, hostIP, hostPort);
                QueuePacket(clientAddress, game);
            }
        }
        // @func SpawnPlayer
        // @desc Spawns a new player and returns it
        public int SpawnPlayer()
        {
            GameObject gameObject = Instantiate(spawnPlayerPrefab);
            gameObject.transform.position = new Vector3(0, 1, 0);
            int serverId = gameObject.GetInstanceID();
            PutEntity(serverId, gameObject);
            return serverId;
        }
    }
}

