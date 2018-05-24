using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace FpsClient {
    // @class Game
    // @desc Simulates the game state on the client side. 
    public class GameClient : Game
    {
        public int mainPlayerServerId;
        public FpsClient.Client m_client;
        public string gameSceneName = "MainScene";
        public string startingSceneName = "MainMenu";

        //Prefabs for players 
        public GameObject playerPrefab;
        public GameObject m_mainPlayer;
        // The server ID of the main player.
        public int ServerId { get; set; }
        //String name of the main player.
        public string MainPlayerName { get; set; }
        //String name of the lobby the main player is in
        public string CurrentLobby { get; set; }


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

      
        public void Update() {  }

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

        // @func NetEvent.Snapshot
        // @desc If this is called, the game has received a snapshot. 
        public override void NetEvent(Netcode.Snapshot snapshot)
        {
            GameObject gameObject;

            if (!m_objects.ContainsKey(snapshot.m_serverId))
            {
                gameObject = SpawnPlayer(snapshot);
            }
            else
            {
                gameObject = GetEntity(snapshot.m_serverId);
                snapshot.Apply(ref gameObject);
            }
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
                case Netcode.PacketType.START_GAME:
                    ProcessStartGame(buf);
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

        //TODO
        public void LeaveLobby()
        {

        }


    }
}
