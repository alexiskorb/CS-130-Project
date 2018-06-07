using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using Steamworks;
using System;
namespace FpsClient {
	struct GameInput {
		public KeyCode key_;
		public Netcode.InputBit cmd_;
		public GameInput(KeyCode key, Netcode.InputBit cmd)
		{
			key_ = key;
			cmd_ = cmd;
		}
	}

    // @class Game
    // @desc Simulates the game state on the client side. 
    public class GameClient : Game
    {
        public int mainPlayerServerId;
        public Client m_client;
        public string gameSceneName = "MainScene";
        public string startingSceneName = "MainMenu";

        //used for Steam invites
        public string m_invitedLobby = "";
        public string m_invitedRegion = "";

        //Prefabs for players 
        public GameObject playerPrefab;
        public GameObject bulletPrefab;
        public GameObject m_mainPlayer;
        private List<GameInput> input_ = new List<GameInput>();
        // The server ID of the main player.
        public string RegionServerName { get; set; }
        //String name of the main player.
        private string m_mainPlayerName = "";
        public string MainPlayerName
        {
            get { return m_mainPlayerName; }
            set { m_mainPlayerName = value; }
        }
        //String name of the lobby the main player is in
        private string m_currentLobby = "";
        public string CurrentLobby
        {
            get { return m_currentLobby; }
            set
            {
                Regex alphanumericRegex = new Regex("^[a-zA-Z0-9]*$");
                if (value != "" && alphanumericRegex.IsMatch(value))
                {
                    m_currentLobby = value;
                }
            }
        }


        //State on whether the menu is open, used by the UI
        public bool MenuOpen { get; set; }

        private List<string> m_listOfServers = new List<string>();
        public List<string> ServerList
        {
            get { return m_listOfServers; }
            set { m_listOfServers = value; }
        }
        //List of lobby names 
        private List<string> m_listOfGames = new List<string>();
        public List<string> ListOfGames
        {
            get { return m_listOfGames; }
            set { m_listOfGames = value; }
        }

        //List of players in the lobby a player is in
        private List<string> m_lobbyPlayers = new List<string>();
        public List<string> LobbyPlayers
        {
            get { return m_lobbyPlayers; }
            set { m_lobbyPlayers = value; }
        }

        // Singleton instance of the GameClient.
        private static GameClient m_instance = null;

        // Get instance of the GameClient. 
        public static GameClient Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = FindObjectOfType<GameClient>();
                    if (m_instance == null)
                    {
                        GameObject gm = new GameObject();
                        gm.name = "GameClient";
                        m_instance = gm.AddComponent<GameClient>();
                        DontDestroyOnLoad(gm);
                    }
                }
                return m_instance;
            }
        }

        public int count = 0;
        // Enforce singleton behavior
        void Awake()
        {
            if (m_instance == null)
            {
                m_instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (m_instance != this)
            {
                Destroy(gameObject);
            }

        }

        private void Start()
        {
            input_.Add(new GameInput(KeyCode.Mouse0, Netcode.InputBit.PRIMARY_WEAPON));

            // Initialize Steam Name
            if (SteamManager.Initialized)
            {
                string playerSteamName = SteamFriends.GetPersonaName();
                Debug.Log(playerSteamName);
                m_mainPlayerName = playerSteamName;
                // SendRefreshServerList();
            }
            else
            {
                // Possibly handle steam initialization error
            }
        }

        public void Update()
        {
            foreach (var input in input_) {
                if (Input.GetKey(input.key_))
                    QueueInput(input.cmd_);
            }
        }

        public void OnEnable()
        {
            // Load into main menu at start of game
            SceneManager.LoadScene(startingSceneName);
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
            if (scene.name == gameSceneName)
            {
                Debug.Log("Main Player Instantiated");
                GameObject gameObject = Instantiate(m_mainPlayer);
                m_objects[mainPlayerServerId] = gameObject;
                m_mainPlayer = gameObject;
            }
        }

        // @func NamesSet
        // @desc Returns true if the main player name and current lobby are set
        public bool NamesSet()
        {
            if (MainPlayerName != "" && CurrentLobby != "")
            {
                return true;
            }
            return false;
        }

        // @func GetMainPlayer
        // @desc Gets the game object associated with the main player. 
        public GameObject GetMainPlayer()
        {
            return m_mainPlayer;
        }

        // @func SpawnPlayer
        // @desc Spawns a new player with the given snapshot. 
        private GameObject SpawnPlayer(Netcode.Snapshot snapshot)
        {
            Debug.Log("Spawning Player");
            GameObject gameObject = Instantiate(playerPrefab);
            snapshot.Apply(ref gameObject);
            m_objects[snapshot.m_serverId] = gameObject;
            return gameObject;
        }

        // @func NetEvent.PacketType 
        // @desc If this is called, the game has received a packet, that is not a snapshot
        // The function analyzes the packet type and responds accordingly.
        public override void NetEvent(Netcode.ClientAddress clientAddr, Netcode.PacketType type, byte[] buf)
        {
            Debug.Log("Received " + type.ToString() + " from " + clientAddr.m_ipAddress + ":" + clientAddr.m_port); 
            switch (type)
            {
                
                case Netcode.PacketType.REFRESH_PLAYER_LIST:
                    ProcessRefreshPlayerList(buf);
                    break;
                case Netcode.PacketType.LEAVE_LOBBY:
                    ProcessLeaveLobby(buf);
                    break;
                case Netcode.PacketType.START_GAME:
                    ProcessStartGame(buf);
                    break;
                case Netcode.PacketType.JOIN_LOBBY:
                    ReceiveJoinLobby(buf);
                    break;
                //case Netcode.PacketType.JOIN_INIT:
                //    AcceptJoinInit(clientAddr, buf);
                //    break;
                /*
            case Netcode.PacketType.INVITE_PLAYER:
                ProcessInvitePlayer (buf);
                break;*/
                case Netcode.PacketType.DISCONNECT:
					ProcessDisconnect (buf);
					break;
            }
        }
        
        // @func ProcessResfreshLobbyList
        // @desc When a REFRESH_LOBBY_LIST packet is received, store the updated list of lobbies open.
        public void ProcessRefreshPlayerList(byte[] buf)
        {
            Debug.Log("Receieved RefreshLobbyList packet");
            Netcode.RefreshPlayerList list = Netcode.Serializer.Deserialize<Netcode.RefreshPlayerList>(buf);
            LobbyPlayers = Netcode.Serializer.Deserialize(list.m_listOfPlayers).ToList();
        }
        public void ReceiveJoinLobby(byte[] buf)
        {
            Debug.Log("Received joinlobby from local");
            Netcode.JoinLobby list = Netcode.Serializer.Deserialize<Netcode.JoinLobby>(buf);
            if (WaitingForAck(Netcode.PacketType.JOIN_LOBBY.ToString() + list.m_playerName))
            {
                Debug.Log("Joined localserver lobby");
                RemoveReliablePacket(Netcode.PacketType.JOIN_LOBBY.ToString() + list.m_playerName);
                MainMenuUI.Instance.GoToStartMatchMenu();
            }
        }

        // @func SendRefreshLobbyList
        // @desc Sends a request to the server for an updated lobby list. Called by the UI.
        public void SendRefreshPlayerList()
        {
            Netcode.RefreshPlayerList packet = new Netcode.RefreshPlayerList();
            QueuePacket(packet);
        }
        
        // @func SendJoinLobby
        // @desc Request the server to join a lobby. This function is called by the UI.
        public void SendJoinLobby()
        {
            Netcode.JoinLobby packet = new Netcode.JoinLobby(MainPlayerName);
            Debug.Log("Sending Joinlobby to " + m_client.m_lobbyServerAddr.m_ipAddress + ":" + m_client.m_lobbyServerAddr.m_port);
            AddReliablePacket(Netcode.PacketType.JOIN_LOBBY.ToString() + MainPlayerName, m_client.m_lobbyServerAddr, packet);
        }
        /*
        // @func SendJoinLobbyFromInvite
        // @desc Request the server to join the lobby that the player was invited to. This is called by the UI.
        public void SendJoinLobbyFromInvite()
        {
            Netcode.JoinLobby packet = new Netcode.JoinLobby(MainPlayerName);
            Debug.Log(packet.m_type);
            QueuePacket(packet);
        }*/

        /*
        // @func ProcessInvitePlayer
        // @desc Server will send a INVITE_PLAYER packet when another player asks to invite 
        public void ProcessInvitePlayer(byte[] buf)
        {
            Debug.Log("Receieved InvitePlayer packet");
            Netcode.InvitePlayer invitation = Netcode.Serializer.Deserialize<Netcode.InvitePlayer>(buf);
            string text = "User " + invitation.m_hostSteamName + " has invited you to join a match with them.";
            SteamJoinMatchUI.Instance.SetMatchText(text);
            MainMenuUI.Instance.OpenSteamJoinMatchPopup();
            m_invitedLobby = invitation.m_lobbyName;
        }
        
        public void SendInvitePlayer(string invitedPlayer)
        {
            Netcode.InvitePlayer packet = new Netcode.InvitePlayer(CurrentLobby, MainPlayerName, invitedPlayer);
            Debug.Log(packet.m_type);
            QueuePacket(packet);
        } 
        */
        // @func ProcessStartGame
        // @desc With the START_GAME packet, save the client's new ID, and IP/port of the match server to communicate with.
        public void ProcessStartGame(byte[] buf)
        {
            Debug.Log("Received StartGame packet");
            Netcode.StartGame game = Netcode.Serializer.Deserialize<Netcode.StartGame>(buf);
            if (mainPlayerServerId != game.m_serverId)
            {
                mainPlayerServerId = game.m_serverId;
                EnterMatch(mainPlayerServerId);
                m_client.BeginSnapshots();
            }
            QueuePacket(m_client.m_lobbyServerAddr, buf);
        }

        // @func SendStartGame
        // @desc Tell the server to start the match. Called by the UI.
        public void SendStartGame()
        {
            Debug.Log("Sending StartGame packet");
            Netcode.StartGame packet = new Netcode.StartGame(CurrentLobby);
            QueuePacket(packet);
        }
        // @func EnterMatch
        // @desc Loads the scene for the match. After it loads, OnSceneLoaded will be called,
        // where the player is spawned.
        public void EnterMatch(int serverId)
        {
            Debug.Log("Loading Game Scene");
            mainPlayerServerId = serverId;
            SceneManager.LoadScene(gameSceneName);
        }
        // @func ProcessLeaveLobby
        // @desc Server will send a LEAVE_LOBBY packet every time a new player leaves the lobby. In that case, update local list of players in the lobby
        public void ProcessLeaveLobby(byte[] buf)
        {
            Debug.Log("Receieved LeaveLobby packet");
            Netcode.LeaveLobby lobby = Netcode.Serializer.Deserialize<Netcode.LeaveLobby>(buf);
            if(lobby.m_playerName == MainPlayerName && WaitingForAck(Netcode.PacketType.LEAVE_LOBBY.ToString()))
            {
                RemoveReliablePacket(Netcode.PacketType.LEAVE_LOBBY.ToString());
            }
            else
            {
                m_lobbyPlayers.Remove(lobby.m_playerName);
            }
        }
        //public void AcceptJoinInit(Netcode.ClientAddress sender, byte[] buf)
        //{
        //    QueuePacket(sender, buf);
        //    Netcode.JoinInit lobby = Netcode.Serializer.Deserialize<Netcode.JoinInit>(buf);
        //    if (m_waitingForInit && m_client.m_lobbyServerAddr.m_ipAddress == sender.m_ipAddress && m_client.m_lobbyServerAddr.m_port == sender.m_port)
        //    {
        //        SendJoinLobby();
        //        m_waitingForInit = false;
        //    }

        //}
        public void SendLeaveLobby()
        {
            Debug.Log("Sending leave lobby packet");
            Netcode.LeaveLobby packet = new Netcode.LeaveLobby(MainPlayerName);
            AddReliablePacket(Netcode.PacketType.LEAVE_LOBBY.ToString(), m_client.m_lobbyServerAddr, packet);
            m_currentLobby = "";
            m_lobbyPlayers.Clear();
        }
        // @func SendDropMatch
        // @desc When player wants to drop match, send DISCONNECT packet to server. Called by UI.
        public void SendDropMatch()
        {
            Debug.Log("Sending disconnect packet");
            Netcode.Disconnect packet = new Netcode.Disconnect(mainPlayerServerId, m_mainPlayerName);
            AddReliablePacket(Netcode.PacketType.DISCONNECT.ToString() + mainPlayerServerId.ToString() + m_mainPlayerName, m_client.m_lobbyServerAddr, packet);
        }

        // @func ProcessDisconnect
        // @desc When player wants to drop match, send DISCONNECT packet to server. Called by UI.
        public void ProcessDisconnect(byte[] buf)
        {
            Debug.Log("Received disconnect packet");
            Netcode.Disconnect disconnect = Netcode.Serializer.Deserialize<Netcode.Disconnect>(buf);
            if (WaitingForAck(Netcode.PacketType.DISCONNECT.ToString() + disconnect.m_serverId.ToString() + disconnect.m_playerName))
            {
                RemoveReliablePacket(Netcode.PacketType.DISCONNECT.ToString() + disconnect.m_serverId.ToString() + disconnect.m_playerName);
                m_currentLobby = "";
                m_lobbyPlayers.Clear();
                mainPlayerServerId = -1;
                KillEntity(mainPlayerServerId);
                m_client.StopSnapshots();
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                Debug.Log("Trying to Disconnect player");
                QueuePacket(m_client.m_lobbyServerAddr, buf);
                if(m_lobbyPlayers.Contains(disconnect.m_playerName))
                {
                    Debug.Log("Removed Disconnecting player");
                    m_lobbyPlayers.Remove(disconnect.m_playerName);
                    KillEntity(disconnect.m_serverId);
                }
            }
        }
        public override void MasterServerEvent(byte[] buf)
        {
            string s = System.Text.Encoding.UTF8.GetString(buf, 0, buf.Length);
            string com = s.Substring(0, 5);
            string arg = s.Substring(6, s.Length - 6);
            Debug.Log("Received packet from master");
            Debug.Log(s);
            switch (com)
            {
                case "pjack": 
                    ReceivePlayerJoin(arg);
                    break;
                case "slack":
                    ReceiveCreateLobby(arg);
                    break;
                case "plack":
                    ReceiveLobbyList(arg);
                    break;
                case "psack":
                    ReceiveServerList(arg);
                    break;
                case "piack":
                    SendPlayerInviteAck(arg);
                    break;
                case "pinvi":
                    ReceivePlayerInvite(arg);
                    break;
                case "slerr":
                    StartLobbyError(arg);
                    break;
                case "pserr":
                    ServerListError(arg);
                    break;
                default:
                    break;
            }
        }
        // @func ReceiveJoinLobby
        // @desc Receive IP and Port info of the lobby player is joining.
        public void ReceivePlayerJoin(string buf)
        {
            Debug.Log("Received Joinlobby ACK from masterserver");
            string[] data = buf.Split(':');
            if (MainPlayerName == data[0])
            {
                Debug.Log("Received Joinlobby ACK matched name");
                if (WaitingForAck("pjoin " + data[0]))
                    RemoveReliablePacket("pjoin " + data[0]);
                m_client.m_lobbyServerAddr = new Netcode.ClientAddress(data[1], Convert.ToInt32(data[2]));
                Debug.Log(m_client.m_lobbyServerAddr.m_ipAddress);
                Debug.Log(m_client.m_lobbyServerAddr.m_port);
                //Netcode.JoinInit lobby = new Netcode.JoinInit(MainPlayerName);
                //QueuePacket(m_client.m_lobbyServerAddr, lobby);
                SendJoinLobby();
            }
        }

        // @func ReceiveCreateLobby
        // @desc ACK for creating the lobby. 
        public void ReceiveCreateLobby(string buf)
        {
            Debug.Log("Received Createlobby ack from master");
            Debug.Log(WaitingForAck("stlob " + buf));
            if (WaitingForAck("stlob " + buf))
            {
                RemoveReliablePacket("stlob " + buf);
                SendPlayerJoinToMaster();
            }
        }
        // @func ReceiveLobbyList
        // @desc Masterserver sent a list of open lobbies. Update list.
        public void ReceiveLobbyList(string buf)
        {
            Debug.Log("Refreshed Lobbies");
            ListOfGames = new List<string>();
            string[] data = buf.Split(':');
            foreach (string entry in data)
            {
                if (entry != "")
                {
                    ListOfGames.Add(entry);
                }
            }
        }
        public void ReceiveServerList(string buf)
        {
            if (WaitingForAck("pslis "+ MainPlayerName))
            {
                RemoveReliablePacket("pslis " + MainPlayerName);

                Debug.Log("Received Server List");
                ServerList = new List<string>();
                string[] data = buf.Split(':');
                foreach (string entry in data)
                {
                    if (entry != "")
                    {
                        ServerList.Add(entry);
                    }
                }
            }

        }
        public void ReceivePlayerInvite(string buf)
        {
            string[] data = buf.Split(':');
            string m_hostSteamName = data[0];
            if(m_invitedLobby != data[3] && m_invitedRegion != data[2])
            {
                //Handle Invite
                string text = "User " + m_hostSteamName + " has invited you to join a match with them.";
                SteamJoinMatchUI.Instance.SetMatchText(text);
                m_invitedRegion = data[2];
                m_invitedLobby = data[3];
                MainMenuUI.Instance.OpenSteamJoinMatchPopup();
            }
            QueuePacket(m_client.MasterServer, "piack " + buf);
        }

        // @func SendCreateLobby
        // @desc Player created a lobby. Send info to masterserver. Function called by UI.
        public void SendCreateLobby()
        {
            string commandName = "stlob ";
            string buffer = commandName + RegionServerName + ":" + CurrentLobby;
            Debug.Log(buffer);
            AddReliablePacket(buffer, m_client.MasterServer, buffer);
        }
        // @func SendRefreshServerList
        // @desc Called by the client when they start the game, to get list of regional servers. 
        public void SendRefreshServerList()
        {
            string commandName = "pslis ";
            Debug.Log("Refreshing ServerList with masterserver");
            //Send player steamID here
            Debug.Log("Sending to " + m_client.MasterServer.m_ipAddress + " " + m_client.MasterServer.m_port);
            AddReliablePacket(commandName + MainPlayerName, m_client.MasterServer, commandName + MainPlayerName);
            RegionServerName = "USW";
        }
        // @func SendRefreshLobbyList
        // @desc Called by the client to get list of lobbies in a server. Called by the UI.
        public void SendRefreshLobbyList()
        {
            string commandName = "pllis ";
            string message = commandName + RegionServerName;
            QueuePacket(m_client.MasterServer, message);
        }
        // @func SendJoinLobby
        // @desc Called when player wants to join the game. Called by the UI.
        public void SendPlayerJoinToMaster()
        {
            string commandName = "pjoin ";
            string buffer = commandName + MainPlayerName + ":" + RegionServerName + ":" + CurrentLobby;
            AddReliablePacket(commandName + MainPlayerName, m_client.MasterServer, buffer);
        }
        // @func SendJoinLobby
        // @desc Overloaded SendPlayerJoin when you want to specify what region:lobby they want to join
        // Used when accepting player invites
        public void SendPlayerJoinFromInvite()
        {
            string commandName = "pjoin ";
            string buffer = commandName + MainPlayerName + ":" + m_invitedRegion + ":" + m_invitedLobby;
            AddReliablePacket(commandName + MainPlayerName, m_client.MasterServer, buffer);
        }
        public void SendPlayerInvite(string steamID)
        {
            string commandName = "pinvi ";
            string buffer = commandName + MainPlayerName + ":" + steamID;
            AddReliablePacket(buffer, m_client.MasterServer, buffer);
        }
        public void SendPlayerInviteAck(string buf)
        {
            if (WaitingForAck("pinvi " + buf))
                RemoveReliablePacket("pinvi " + buf);
        }
        public void StartLobbyError(string buf)
        {
            if (WaitingForAck("stlob " + buf))
            {
                RemoveReliablePacket("stlob " + buf);
            }
            MainMenuUI.Instance.createMatchMenu.GetComponent<CreateMatchUI>().ShowCreateMatchError();
        }
        public void ServerListError(string buf)
        {
            if (WaitingForAck("psack " + buf))
            {
                RemoveReliablePacket("psack " + buf);
            }
        }

        private GameObject SpawnBullet(Netcode.BulletSnapshot bulletState)
		{
			GameObject gameObject = Instantiate(bulletPrefab);
			bulletState.Apply(ref gameObject);
			PutEntity(bulletState.m_serverId, gameObject);
			return gameObject;
		}

        public override void NetEvent(Netcode.PlayerInput playerInput)
        {
            throw new System.NotImplementedException();
        }

        public override void NetEvent(Netcode.BulletSnapshot bullet)
        {
            GameObject gameObject;

			if (!m_objects.ContainsKey(bullet.m_serverId)) {
				gameObject = SpawnBullet(bullet);
			} else {
				gameObject = GetEntity(bullet.m_serverId);
				if (gameObject == null) {
					KillEntity(bullet.m_serverId);
				} else {
					bullet.Apply(ref gameObject);
				}
			}
		}

        public override void NetEvent(Netcode.PlayerSnapshot player)
        {
            GameObject gameObject;

            if (m_objects.ContainsKey(player.m_serverId))
            {
                gameObject = GetEntity(player.m_serverId);
                player.Apply(ref gameObject);
            }
        }

        // @func NetEvent.Snapshot
        // @desc If this is called, the game has received a snapshot. 
        public override void NetEvent(Netcode.Snapshot snapshot)
        {
            GameObject gameObject;

			if (!m_objects.ContainsKey(snapshot.m_serverId)) {
				gameObject = SpawnPlayer(snapshot);
			} else {
				gameObject = GetEntity(snapshot.m_serverId);
				snapshot.Apply(ref gameObject);
			}
		}
	}
}
