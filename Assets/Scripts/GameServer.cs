using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace FpsServer {
	// @class Game
	// @desc Simulates the game state on the server side.
	// The game objects on the server side are hashed by their Unity instance IDs. 
	public class GameServer : Game {
		public GameObject spawnPlayerPrefab;
        private Dictionary<string, List<string>> m_listOfMatches = new Dictionary<string, List<string>>();
        private Dictionary<string, Netcode.ClientAddress> m_clientAddresses= new Dictionary<string, Netcode.ClientAddress>();
        public Server m_server;
        public List<string> activeMatchPlayers = new List<string>();
        public string activeMatchName;
        public string activeHostIp;
        public int activeHostPort;

        public string gameSceneName = "ServerMainScene";

        public void Update() {}
        public void OnEnable()
        {
            DontDestroyOnLoad(this.gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Unsubscribe from sceneLoaded event
        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // Used to call relevant functions after the scene loads since scene loads complete in the frame after they're called
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (string client in activeMatchPlayers)
            {
                Netcode.ClientAddress clientAddress = m_clientAddresses[client];
                int serverId = SpawnPlayer();
                m_server.ServerIds[clientAddress] = serverId;
                m_server.Clients[clientAddress] = new Netcode.SnapshotHistory<Netcode.Snapshot>();
                Netcode.StartGame game = new Netcode.StartGame(activeMatchName, serverId, activeHostIp, activeHostPort);
                QueuePacket(clientAddress, game);
            }
        }

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
                case Netcode.PacketType.LEAVE_LOBBY:
                    ProcessLeaveLobby(clientAddr, buf);
                    break;
                case Netcode.PacketType.START_GAME:
                    ProcessStartGame(buf);
                    break;
				case Netcode.PacketType.INVITE_PLAYER:
					ProcessInvitePlayer (buf);
					break;
            }
        }

        // @func RefreshLobbyList
        // @desc A client asked for LobbyList. Sends the packet to the client.
        public void ProcessRefreshLobbyList(Netcode.ClientAddress clientAddr)
        {
            Debug.Log("Received RefreshLobby packet");
            string[] lobbyList = m_listOfMatches.Keys.ToArray();
            Netcode.RefreshLobbyList packet = new Netcode.RefreshLobbyList(lobbyList);
            QueuePacket(clientAddr, packet);
        }

        // @func ProcessCreateLobby
        // @desc Client wants to create a lobby. Add it to the list of lobbies, then reply back 
        // to the client with a JoinLobby confirmation packet.
        public void ProcessCreateLobby(Netcode.ClientAddress clientAddr, byte[] buf)
        {
            Debug.Log("Received CreateLobby packet");
            Netcode.CreateLobby lobby = Netcode.Serializer.Deserialize<Netcode.CreateLobby>(buf);
            m_listOfMatches.Add(lobby.m_lobbyName, new List<string> { lobby.m_hostPlayerName });
            m_clientAddresses[lobby.m_hostPlayerName] = clientAddr;
            SendJoinLobby(lobby.m_lobbyName);
        }

        // @func ProcessJoinLobby
        // @desc A client requests to join a lobby. Add the player to the list, and store player string 
        // and connection info. Send a packet to all players in that lobby with an updated list of players
        // in the lobby.
        public void ProcessJoinLobby(Netcode.ClientAddress clientAddr, byte[] buf)
        {
            Debug.Log("Received JoinLobby packet");
            Netcode.JoinLobby lobby = Netcode.Serializer.Deserialize<Netcode.JoinLobby>(buf);
            if(m_listOfMatches.ContainsKey(lobby.m_lobbyName))
            {
                m_listOfMatches[lobby.m_lobbyName].Add(lobby.m_playerName);
                m_clientAddresses[lobby.m_playerName] = clientAddr;
                SendJoinLobby(lobby.m_lobbyName);
            }
        }
        public void SendJoinLobby(string lobbyName)
        {
            Debug.Log("Sending JoinLobby packet");
            Netcode.JoinLobby packet = new Netcode.JoinLobby(m_listOfMatches[lobbyName].ToArray(), lobbyName, "");
            foreach (string player in m_listOfMatches[lobbyName])
            {
                QueuePacket(m_clientAddresses[player], packet);
            }
        }

        // @func ProcessLeaveLobby
        // @desc A client requests to leave a lobby. Remove the player from the list.
        // If the lobby is non-empty, send a packet to all players in that lobby with an
        // updated list of players in the lobby.
        // Otherwise, remove the lobby from the list of matches.
        public void ProcessLeaveLobby(Netcode.ClientAddress clientAddr, byte[] buf)
        {
            Debug.Log("Received LeaveLobby packet");
            Netcode.LeaveLobby lobby = Netcode.Serializer.Deserialize<Netcode.LeaveLobby>(buf);
            if (m_listOfMatches[lobby.m_lobbyName].Contains(lobby.m_playerName))
            {
                m_listOfMatches[lobby.m_lobbyName].Remove(lobby.m_playerName);
                if (m_listOfMatches[lobby.m_lobbyName].Any())
                {
                    SendLeaveLobby(lobby.m_lobbyName);
                }
                else
                {
                    Debug.Log("Removing Lobby " + lobby.m_lobbyName);
                    m_listOfMatches.Remove(lobby.m_lobbyName);
                }
            }
        }
        public void SendLeaveLobby(string lobbyName)
        {
            Debug.Log("Sending LeaveLobby packet");
            Netcode.LeaveLobby packet = new Netcode.LeaveLobby(m_listOfMatches[lobbyName].ToArray(), lobbyName, "");
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
            string hostIP = "127.0.0.1";
            int hostPort = 9001;
            //Send list of names and IP to new server
            activeHostIp = hostIP;
            activeHostPort = hostPort;
            activeMatchPlayers = matchPlayers;
            activeMatchName = matchName;
            EnterMatch();
        }

		// @func ProcessInvitePlayer
		// @desc A client wants to invite another player to join their existing lobby. Find the player based on their
		// name and send them a packet with the lobby information and the source of the invite.
		public void ProcessInvitePlayer(byte[] buf)
		{
			Debug.Log("Received InvitePlayer packet");
			Netcode.InvitePlayer invitation = Netcode.Serializer.Deserialize<Netcode.InvitePlayer>(buf);
			if(m_listOfMatches.ContainsKey(invitation.m_lobbyName))
			{
				if (m_clientAddresses.ContainsKey(invitation.m_invitedSteamName))
				{
					SendInvitePlayer (invitation.m_lobbyName, invitation.m_hostSteamName, invitation.m_invitedSteamName);
				}
			}
		}
		public void SendInvitePlayer(string lobbyName, string hostPlayer, string invitedPlayer)
		{
			Debug.Log("Sending InvitePlayer packet");
			Netcode.InvitePlayer packet = new Netcode.InvitePlayer(lobbyName, hostPlayer, invitedPlayer);
			QueuePacket(m_clientAddresses[invitedPlayer], packet);
		}

        //********************************
        //This will be called by the server running the instance of the match

        // @func SpawnPlayer
        // @desc Spawns a new player and returns it
        public int SpawnPlayer()
        {
            Debug.Log("Spawning Player");
            GameObject gameObject = Instantiate(spawnPlayerPrefab);
            gameObject.transform.position = new Vector3(0, 1, 0);
            int serverId = gameObject.GetInstanceID();
            PutEntity(serverId, gameObject);
            return serverId;
        }
/*
        // @func RunMatch
        // @desc The match server will initialize a gameobject for each client, and send every client in the match
        // their unique ServerID, and the hostIP and hostPort to start communicating with.
        public void RunMatch(string matchName, List<string> players)
        {
            string hostIP;
            int hostPort;
            
        }
        */

        public void EnterMatch()
        {
            SceneManager.LoadScene(gameSceneName);
        }

		public override void NetEvent(Netcode.PlayerInput playerInput)
		{
			GameObject gameObject = GetEntity(playerInput.serverId_);
			if (gameObject == null)
				return;
			NetworkedPlayer networkedPlayer = gameObject.GetComponent<NetworkedPlayer>();
			networkedPlayer.TakeCommands(playerInput.cmdBits_);
		}

		public override void NetEvent(Netcode.BulletSnapshot bulletSnapshot)
		{
			throw new System.NotImplementedException();
		}

        public override void NetEvent(Netcode.PlayerSnapshot player)
        {
            throw new System.NotImplementedException();
        }
    }
}

