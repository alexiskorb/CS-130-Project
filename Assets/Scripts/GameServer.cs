using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
namespace FpsServer {
	// @class Game
	// @desc Simulates the game state on the server side.
	// The game objects on the server side are hashed by their Unity instance IDs. 
	public class GameServer : Game {
		public GameObject spawnPlayerPrefab;
		public uint PREDICTION_BUFFER_SIZE = 20;


        //This structure saves all of the connected player names and their network addresses.
        //The ClientAddress value can be a null type, which indicates that a player has joined on MasterServer but hasn't
        //Made connection with the GameServer itself yet.
        private Dictionary<string, Netcode.ClientAddress?> m_clientAddresses= new Dictionary<string, Netcode.ClientAddress?>();
        public Server m_server;
        public string gameSceneName = "ServerMainScene";
        public string RegionServerName { get; set; }
        public string CurrentLobby { get; set; }
        private bool inMatch = false;
		public List<Vector3> m_spawnPoints = new List<Vector3> {
			new Vector3(-10.0f, 1f, -10.0f),
			new Vector3(-10.0f, 1f, 10.0f),
			new Vector3(10.0f, 1f, 10.0f),
			new Vector3(10.0f, 1f, -10.0f)
		};
        //GameServer lets the masterserver know its online.
        private void Start()
        {
            SendRegisterServer();            
        }
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
            // The ServerStandby scene is loaded when a gameserver is connected to the masterserver, but has no match running.
            if (scene.name == "ServerStandby")
            {
                inMatch = false;
            }
            else
            {
                // When the match starts, spawn all connected players, and tell every player that the match has started. 
                foreach (string client in m_clientAddresses.Keys)
                {
                    Netcode.ClientAddress clientAddress = m_clientAddresses[client].Value;
                    int serverId = SpawnPlayer();
                    m_server.ServerIds[clientAddress] = serverId;
                    m_server.Clients[clientAddress] = new Netcode.SnapshotHistory<Netcode.Snapshot>(PREDICTION_BUFFER_SIZE);
                    Netcode.StartGame game = new Netcode.StartGame(CurrentLobby, serverId);
                    AddReliablePacket(Netcode.PacketType.START_GAME.ToString() + serverId.ToString(), clientAddress, game);
                }
            }
        }

        // @func PutSnapshot
        // @desc The server received a snapshot. 
        public override void NetEvent(Netcode.Snapshot snapshot)
		{
			GameObject gameObject = GetEntity(snapshot.m_serverId);
			snapshot.Apply(ref gameObject);
		}

        // @func NetEvent.PacketType 
        // @desc If this is called, the game has received a packet from a player, that is not a snapshot
        // The function analyzes the packet type and responds accordingly.
        public override void NetEvent(Netcode.ClientAddress clientAddr, Netcode.PacketType type, byte[] buf)
		{
            Debug.Log("Received " + type.ToString() + " from " + clientAddr.m_ipAddress +":" +  clientAddr.m_port);
            switch (type)
            {
                case Netcode.PacketType.REFRESH_PLAYER_LIST:
                    ProcessRefreshPlayerList(clientAddr);
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
				case Netcode.PacketType.DISCONNECT:
					ProcessDisconnect (clientAddr, buf);
					break;

            }
        }

        // @func ReceiveRefreshPlayerList
        // @desc A client asked for LobbyList. Sends the packet to the client.
        public void ProcessRefreshPlayerList(Netcode.ClientAddress clientAddr)
        {
            Debug.Log("Received RefreshPlayerList packet");
            string[] lobbyList = m_clientAddresses.Keys.ToArray();
            Netcode.RefreshPlayerList packet = new Netcode.RefreshPlayerList(lobbyList);
            QueuePacket(clientAddr, packet);
        }

        // @func ProcessJoinLobby
        // @desc A client requests to join a lobby. Add the player to the list, and store player string 
        // and connection info. Send a packet to all players in that lobby with an updated list of players
        // in the lobby.
        public void ProcessJoinLobby(Netcode.ClientAddress clientAddr, byte[] buf)
        {
            Debug.Log("Received JoinLobby packet from " + clientAddr.m_ipAddress + clientAddr.m_port);
            Netcode.JoinLobby lobby = Netcode.Serializer.Deserialize<Netcode.JoinLobby>(buf);
            if(m_clientAddresses.ContainsKey(lobby.m_playerName) && !m_clientAddresses[lobby.m_playerName].HasValue)
            {
                if (WaitingForAck("ABC" + lobby.m_playerName))
                    RemoveReliablePacket("ABC" + lobby.m_playerName);
                m_clientAddresses[lobby.m_playerName] = clientAddr;
                foreach(string player in m_clientAddresses.Keys)
                {
                    if(m_clientAddresses[player].HasValue)
                    {
                        ProcessRefreshPlayerList(m_clientAddresses[player].Value);
                    }
                }
            }
            QueuePacket(clientAddr, buf);
        }

        // @func ProcessLeaveLobby
        // @desc A client requests to leave a lobby. Remove the player from the list.
        // If the lobby is non-empty, send a packet to all players in that lobby with an
        // updated list of players in the lobby.
        // Tell the masterserver the player left.
        public void ProcessLeaveLobby(Netcode.ClientAddress clientAddr, byte[] buf)
        {
            Debug.Log("Received LeaveLobby packet");
            Netcode.LeaveLobby lobby = Netcode.Serializer.Deserialize<Netcode.LeaveLobby>(buf);
            QueuePacket(clientAddr, buf);
            if (m_clientAddresses.ContainsKey(lobby.m_playerName))
            {
                m_clientAddresses.Remove(lobby.m_playerName);
                SendLeaveLobby(lobby.m_playerName);
                SendPlayerQuit(lobby.m_playerName);
            }
        }
        // @func SendLeaveLobby
        // @desc Notify every player connected to the lobby that another player left.
        public void SendLeaveLobby(string playerName)
        {
            Debug.Log("Sending LeaveLobby packet");
            Netcode.LeaveLobby packet = new Netcode.LeaveLobby(playerName);
            foreach (string player in m_clientAddresses.Keys)
            {
                if(m_clientAddresses[player].HasValue)
                    QueuePacket(m_clientAddresses[player].Value, packet);
            }
        }

        // @func ProcessStartGame
        // @desc A client requests to begin a match from an existing lobby. Find/Create a server to handle the match,
        // and get its IP & port. Give the list of players and their ClientAddress off to the new match server.
        public void ProcessStartGame(byte[] buf)
        {
            Netcode.StartGame game = Netcode.Serializer.Deserialize<Netcode.StartGame>(buf);
            string matchName = game.m_matchName;
            CurrentLobby = matchName;
            if (WaitingForAck(Netcode.PacketType.START_GAME.ToString() + game.m_serverId.ToString()))
            {
                RemoveReliablePacket(Netcode.PacketType.START_GAME.ToString() + game.m_serverId.ToString());
            }
            if(!inMatch)
                EnterMatch();
        }

        // @func ProcessDisconnect
        // @desc A client wants to drop the game and disconnect.
        public void ProcessDisconnect(Netcode.ClientAddress clientAddr, byte[] buf)
		{
			Debug.Log("Received Disconnect");
			Netcode.Disconnect disconnect = Netcode.Serializer.Deserialize<Netcode.Disconnect>(buf);
            string clientAddressString = clientAddr.m_ipAddress + clientAddr.m_port.ToString();
            if (WaitingForAck(Netcode.PacketType.DISCONNECT.ToString() + disconnect.m_serverId.ToString() + clientAddressString))
            {
                RemoveReliablePacket(Netcode.PacketType.DISCONNECT.ToString() + disconnect.m_serverId.ToString() + clientAddressString);
            }
            else if (m_clientAddresses.ContainsKey(disconnect.m_playerName))
            {
                Debug.Log("Destroying" + disconnect.m_playerName);
                QueuePacket(clientAddr, buf);
                SendDisconnect(disconnect.m_serverId, disconnect.m_playerName);
                Netcode.ClientAddress addr = m_clientAddresses[disconnect.m_playerName].Value;
                KillEntity(disconnect.m_serverId);
                m_clientAddresses.Remove(disconnect.m_playerName);
                m_server.RemoveClient(addr);
                SendPlayerQuit(disconnect.m_playerName);
                Debug.Log(m_clientAddresses.Count);
                if (m_clientAddresses.Count == 0)
                {
                    SendRegisterServer();
                }
            }
            else
                QueuePacket(clientAddr, buf);
        }
        // @func SendDisconnect
        // @desc Tells all players that a player disconnected.
        // This packet is sent reliably.
        public void SendDisconnect(int serverId, string name)
		{
			Debug.Log ("Sending Disconnect packet");
			Netcode.Disconnect packet = new Netcode.Disconnect (serverId, name);
            foreach (var client in m_clientAddresses.Keys)
            {
                Debug.Log("Sending disconnect of " + name + " to " + client);
                string clientAddress = m_clientAddresses[client].Value.m_ipAddress + m_clientAddresses[client].Value.m_port.ToString();
                AddReliablePacket(Netcode.PacketType.DISCONNECT.ToString() + packet.m_serverId.ToString() + clientAddress, m_clientAddresses[client].Value, packet);
            }
        }

        // @func SpawnPlayer
        // @desc Spawns a new player and returns it
        public int SpawnPlayer()
        {
            Debug.Log("Spawning Player");
            GameObject gameObject = Instantiate(spawnPlayerPrefab);
			if (m_spawnPoints.Count > 0) {
				gameObject.transform.position = m_spawnPoints[0];
				m_spawnPoints.RemoveAt(0);
			} else {
				gameObject.transform.position = new Vector3(0f, 1f, 0f);
			}

			int serverId = gameObject.GetInstanceID();
            PutEntity(serverId, gameObject);
            return serverId;
        }
        // @func EnterMatch
        // @desc Start the match. Notify the masterserver that it is starting a match with SendClose.
        // If there are any players that are "joined" on the masterserver but hasn't established connection with
        // the GameServer directly, remove them and notify the masterserver.
        public void EnterMatch()
        {
            inMatch = true;
            SendClose();
            foreach (string player in m_clientAddresses.Keys)
            {
                if(!m_clientAddresses[player].HasValue)
                {
                    m_clientAddresses.Remove(player);
                    Debug.Log(player + " removed for not joining before match start");
                }
            }
            SceneManager.LoadScene(gameSceneName);
        }

        // @func MasterServerEvent
        // @desc Handles packets received from the MasterServer
        // Communication with the masterserver uses strings where the first 5 characters denote the command name
        public override void MasterServerEvent(byte[] buf)
        {
            string message = System.Text.Encoding.UTF8.GetString(buf, 0, buf.Length);
            string com = message.Substring(0, 5);
            string arg = message.Substring(6, message.Length - 6);
            Debug.Log(message);
            switch (com)
            {
                case "ssack":
                    ReceiveRegisterServer(arg);
                    break;
                case "stlob":
                    ReceieveCreateLobby(arg);
                    break;
                case "pjoin":
                    ReceivePlayerJoin(arg);
                    break;
                case "clack":
                    ReceiveClose(arg);
                    break;
                case "pqack":
                    ReceivePlayerQuit(arg);
                    break;
                default:
                    Debug.Log("Unrecognized command from masterserver. Dropping...");
                    break;
            }
        }
        // @func ReceiveRegisterServer
        // @desc An ACK message from the masterserver that the server was registered.
        public void ReceiveRegisterServer(string buf)
        {
            string message = "stser " + buf;
            if (WaitingForAck(message))
            {
                RemoveReliablePacket(message);
                SceneManager.LoadScene("ServerStandby");
            }
        }
        // @func ReceiveCreateLobby
        // @desc Lobby was created on the server. Send an ACK to masterserver for confirmation of the packet.
        public void ReceieveCreateLobby(string buf)
        {
            Debug.Log("Received CreateLobby from masterserver");
            if(CurrentLobby != null)
            {
                //Send ACK
                string buffer = "slack " + buf;
                QueuePacket(m_server.MasterServer, buffer);
            }
            else
            {
                string[] data = buf.Split(':');
                if (RegionServerName == data[0])
                    CurrentLobby = data[1];
                else
                    Debug.Log("CreateLobby received wrong region name");
                string buffer = "slack " + buf;
                QueuePacket(m_server.MasterServer, buffer);
            }      
        }
        // @func ReceivePlayerJoin
        // @desc Masterserver notified a player has joined the lobby. Send an ACK for confirmation.
        public void ReceivePlayerJoin(string buf)
        {
            string[] data = buf.Split(':');
            if (m_clientAddresses.ContainsKey(data[0]))
            {
                //SendAck
                QueuePacket(m_server.MasterServer, "pjack " + buf);
            }
            else
            {
                if (RegionServerName == data[3] && CurrentLobby == data[4])
                {
                    m_clientAddresses[data[0]] = null;
                    Netcode.ClientAddress tempPlayer = new Netcode.ClientAddress(data[1], Convert.ToInt32(data[2]));
                    string init = "ABC";
                    AddReliablePacket(init + data[0], tempPlayer, init);
                    }
                else
                    Debug.Log("PlayerJoin received wrong lobby name");
                QueuePacket(m_server.MasterServer, "pjack " + buf);

            }

        }

        // @func ReceiveClose
        // @desc The lobby was closed. Process Acks from masterserver.
        public void ReceiveClose(string buf)
        {
            string message = "close " + buf;
            if (WaitingForAck(message))
            {
                RemoveReliablePacket(message);
            }
        }
        // @func ReceivePlayerQuit
        // @desc ACK by the masterserver after SendPlayerQuit.
        public void ReceivePlayerQuit(string buf)
        {
            string message = "pquit " + buf;
            if (WaitingForAck(message))
            {
                RemoveReliablePacket(message);
            }
        }
        // @func SendRegisterServer
        // @desc When server starts, send the name of the region the server is located in. Called by the UI.
        public void SendRegisterServer()
        {
            string commandName = "stser ";
            RegionServerName = "USW";
            string buffer = commandName + RegionServerName;
            AddReliablePacket(buffer, m_server.MasterServer, buffer);
        }

        // @func SendPlayerQuit
        // @desc When a client leaves, notify masterserver. 
        public void SendPlayerQuit(string playerName)
        {
            string commandName = "pquit ";
            string buffer = commandName + playerName;
            AddReliablePacket(buffer, m_server.MasterServer, buffer);
        }
        // @func SendClose
        // @desc A player closed the lobby. Notify the masterserver
        public void SendClose()
        {
            string commandName = "close ";
            string regionLobby = RegionServerName + ":" + CurrentLobby;
            string buffer = commandName + regionLobby;
            AddReliablePacket(buffer, m_server.MasterServer, buffer);
        }
        //@func RestartServer
        //@desc Clear all stored info to restart the server.
        public void RestartServer()
        {
            Debug.Log("Closing Lobby, switching scenes");
            CurrentLobby = null;
            m_clientAddresses.Clear();
            m_objects.Clear();
            m_server.m_clients.Clear();
            m_server.m_serverIds.Clear();
            GetPacketQueue();
            GetPacketsForClient();
            GetReliablePackets().Clear();
            SceneManager.LoadScene("ServerStandby");
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

