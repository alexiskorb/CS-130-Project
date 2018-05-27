using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using Steamworks;

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

        //Prefabs for players 
        public GameObject playerPrefab;
		public GameObject bulletPrefab;
		public GameObject m_mainPlayer;
		private List<GameInput> input_ = new List<GameInput>();
		// The server ID of the main player.
		public int ServerId { get; set; }
        //String name of the main player.
        private string m_mainPlayerName = "";
        public string MainPlayerName
        {
            get { return m_mainPlayerName;  }
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

        //IP of the host server the client communicates to in a match
        public string MatchHostIp { get; set; }
        //Port number of the server hosting the match
        public int MatchHostPort { get; set; }
        //State on whether the menu is open, used by the UI
        public bool MenuOpen { get; set; }

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
            switch (type)
            {
                case Netcode.PacketType.REFRESH_LOBBY_LIST:
                    ProcessRefreshLobbyList(buf);
                    break;
                case Netcode.PacketType.JOIN_LOBBY:
                    ProcessJoinLobby(buf);
                    break;
                case Netcode.PacketType.LEAVE_LOBBY:
                    ProcessLeaveLobby(buf);
                    break;
                case Netcode.PacketType.START_GAME:
                    ProcessStartGame(buf);
                    break;
				case Netcode.PacketType.INVITE_PLAYER:
					ProcessInvitePlayer (buf);
					break;
            }
        }

        // @func ProcessResfreshLobbyList
        // @desc When a REFRESH_LOBBY_LIST packet is received, store the updated list of lobbies open.
        public void ProcessRefreshLobbyList(byte[] buf)
        {
            Debug.Log("Receieved RefreshLobbyList packet");
            Netcode.RefreshLobbyList list = Netcode.Serializer.Deserialize<Netcode.RefreshLobbyList>(buf);
            ListOfGames = Netcode.Serializer.Deserialize(list.m_listOfGames).ToList();
        }
        // @func SendRefreshLobbyList
        // @desc Sends a request to the server for an updated lobby list. Called by the UI.
        public void SendRefreshLobbyList()
        {
            Netcode.RefreshLobbyList packet = new Netcode.RefreshLobbyList();
            QueuePacket(packet);
        }
        // @func ProcessJoinLobby
        // @desc Server will send a JOIN_LOBBY packet every time a new player joins the lobby. In that case, update local list of players in the lobby
        public void ProcessJoinLobby(byte[] buf)
        {
            Debug.Log("Receieved JoinLobby packet");
            Netcode.JoinLobby lobby = Netcode.Serializer.Deserialize<Netcode.JoinLobby>(buf);
            CurrentLobby = lobby.m_lobbyName;
            LobbyPlayers = Netcode.Serializer.Deserialize(lobby.m_listOfPlayers).ToList();
        }
        // @func SendJoinLobby
        // @desc Request the server to join a lobby. This function is called by the UI.
        public void SendJoinLobby()
        {
            Netcode.JoinLobby packet = new Netcode.JoinLobby(null, CurrentLobby, MainPlayerName);
            Debug.Log(packet.m_type);
            QueuePacket(packet);
        }
		// @func SendJoinLobbyFromInvite
		// @desc Request the server to join the lobby that the player was invited to. This is called by the UI.
		public void SendJoinLobbyFromInvite()
		{
			Netcode.JoinLobby packet = new Netcode.JoinLobby(null, m_invitedLobby, MainPlayerName);
			Debug.Log(packet.m_type);
			QueuePacket(packet);
		}

		// @func ProcessInvitePlayer
		// @desc Server will send a INVITE_PLAYER packet when another player asks to invite 
		public void ProcessInvitePlayer(byte[] buf)
		{
			Debug.Log("Receieved InvitePlayer packet");
			Netcode.InvitePlayer invitation = Netcode.Serializer.Deserialize<Netcode.InvitePlayer>(buf);
			string text = "User " + invitation.m_hostSteamName + " has invited you to join a match with them.";
			SteamJoinMatchUI.Instance.SetMatchText(text);
			MainMenuUI.Instance.OpenSteamJoinMatchPopup ();
			m_invitedLobby = invitation.m_lobbyName;
		}
		public void SendInvitePlayer(string invitedPlayer)
		{
			Netcode.InvitePlayer packet = new Netcode.InvitePlayer (CurrentLobby, MainPlayerName, invitedPlayer);
			Debug.Log (packet.m_type);
			QueuePacket (packet);
		}

        // @func ProcessStartGame
        // @desc With the START_GAME packet, save the client's new ID, and IP/port of the match server to communicate with.
        public void ProcessStartGame(byte[] buf)
        {
            Debug.Log("Received StartGame packet");
            Netcode.StartGame game = Netcode.Serializer.Deserialize<Netcode.StartGame>(buf);
            ServerId = game.m_serverId;
            MatchHostIp = game.m_hostIP;
            MatchHostPort = game.m_hostPort;
            Debug.Log("Connecting to IP " + MatchHostIp + " at port " + MatchHostPort);
            EnterMatch(ServerId);

            //These two functions makes the client send its snapshots periodically to the server.
            m_client.Tick = new Netcode.PeriodicFunction(m_client.SnapshotTick, 0);
            m_client.m_newSeqno = m_client.NewSeqno;
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

        // @func CreateLobby
        // @desc Ask the server to create a new lobby. Called by the UI. 
        // The client will receive a JOIN_LOBBY response after it succeeds.
        public void CreateLobby()
        {
            Debug.Log("Sending create lobby packet");
            Netcode.CreateLobby packet = new Netcode.CreateLobby(CurrentLobby, MainPlayerName);
            QueuePacket(packet);
            //TODO: Possibly receive ACK for match creation
        }

        // @func ProcessLeaveLobby
        // @desc Server will send a LEAVE_LOBBY packet every time a new player leaves the lobby. In that case, update local list of players in the lobby
        public void ProcessLeaveLobby(byte[] buf)
        {
            Debug.Log("Receieved LeaveLobby packet");
            Netcode.LeaveLobby lobby = Netcode.Serializer.Deserialize<Netcode.LeaveLobby>(buf);
            if (CurrentLobby == lobby.m_lobbyName)
            {
                LobbyPlayers = Netcode.Serializer.Deserialize(lobby.m_listOfPlayers).ToList();
            }
        }

        public void LeaveLobby()
        {
            Debug.Log("Sending leave lobby packet");
            Netcode.LeaveLobby packet = new Netcode.LeaveLobby(null, CurrentLobby, MainPlayerName);
            QueuePacket(packet);
            m_currentLobby = "";
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
