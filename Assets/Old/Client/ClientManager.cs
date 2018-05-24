using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

//Written to prototype the LLAPI for Unet. Code is based on tutorials from Unity's documentation and a tutorial from Jonathon Merefield.

//Client code using the LLAPI. 
public class ClientManager : MonoBehaviour
{
    // Scene to load into at start of game
    public string startingSceneName;
    public string gameSceneName;

    public const int serverPort = 8889;
    public int clientPort;
    public int messagesReceivedPerUpdate = 10;

    // Connection Info
    private int m_connectionID;
    private int m_channelID;
    private int m_hostID;
    private int maxConnections = 4;
    private byte m_error;

    private string m_mainPlayerName;
    private Dictionary<string, GameObject> m_players;
    private List<string> m_openMatches;

    // Singleton instance of the ClientManager
    private static ClientManager m_instance = null;

    // Get instance of the ClientManager
    public static ClientManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<ClientManager>();
                if (m_instance == null)
                {
                    GameObject cm = new GameObject();
                    cm.name = "ClientManager";
                    m_instance = cm.AddComponent<ClientManager>();
                    DontDestroyOnLoad(cm);
                }
            }
            return m_instance;
        }
    }

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

    // Use this for initialization
    void Start()
    {
        // Initialize variables
        m_openMatches = new List<string>();

        // TEMP CODE ---------------
        // Randomize client port number so we can have multiple connections
        Random.InitState((int)(System.DateTime.Now.Minute + System.DateTime.Now.Second));
        clientPort = 8888 + Random.Range(1, 30);
        string portNo = "PortNo:" + clientPort;
        Debug.Log(portNo);
        // END TEMP CODE ------------

        // Start Network Transport
        NetworkTransport.Init();
        Connect();

        // Load into main menu at start of game
        SceneManager.LoadScene(startingSceneName);

    }

    // Update is called once per frame
    void Update()
    {
        // Receive messages from server
        int recHostID;
        int recConnectionID;
        int recChannelID;
        int bufferSize = 1024;
        byte[] recBuffer = new byte[bufferSize];
        int dataSize;

        // Receive and handle messagesReceivedPerUpdate number of messages
        for (int i = 0; i < messagesReceivedPerUpdate; i++)
        {
            NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out m_error);

            switch (recNetworkEvent)
            {
                case NetworkEventType.ConnectEvent:
                    Debug.Log("Connected");
                    break;
                case NetworkEventType.DataEvent:
                    string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                    Debug.Log("Receiving: " + msg);
                    string[] splitData = msg.Split('|');
                    switch (splitData[0])
                    {
                        case "OPEN_MATCH_LIST":
                            UpdateOpenMatches(msg);
                            break;
                        case "PLAYER_LOBBY_UPDATE":
                            UpdatePlayerLobby(msg);
                            break;
                        case "START_MATCH":
                            StartMatch(splitData[1]);
                            break;
                        case "MOVE_PLAYER":
                            MovePlayer(splitData[1], splitData[2], splitData[3], splitData[4]);
                            break;
                    }
                    break;
                case NetworkEventType.DisconnectEvent:
                    Debug.Log("Disconnected");
                    break;
            }
        }

        // Update list of players and send player data to server
        m_players = GameManager.Instance.Players;

        if (GameManager.Instance.InMatch)
        {
			SendPlayerData ();
        }
    }

    // Subscribe to GameManager events
    private void OnEnable()
    {
        GameManager.createMatchEvent += createMatchHandler;
        GameManager.joinMatchEvent += joinMatchHandler;
        GameManager.leaveMatchLobbyEvent += leaveMatchLobbyHandler;
        GameManager.gameLoadedEvent += gameLoadedHandler;
        GameManager.dropMatchEvent += dropMatchHandler;
    }
    // Unsubscribe from GameManager events
    private void OnDisable()
    {
        GameManager.createMatchEvent -= createMatchHandler;
        GameManager.joinMatchEvent -= joinMatchHandler;
        GameManager.leaveMatchLobbyEvent -= leaveMatchLobbyHandler;
        GameManager.gameLoadedEvent += gameLoadedHandler;
        GameManager.dropMatchEvent -= dropMatchHandler;
    }

    // Called automatically when GameManager asks to create a match
    private void createMatchHandler(string playerId, string matchId)
    {
        Debug.Log("GameManager asked to create a match");
        //Connect();
        string msg = "CREATE_MATCH|" + playerId + "|" + matchId;
        sendMessage(msg);
        m_mainPlayerName = GameManager.Instance.MainPlayerName;
    }

    // Called automatically when GameManager asks to join a match
    private void joinMatchHandler(string playerId, string matchId)
    {
        Debug.Log("GameManager asked to join a match");
        string msg = "JOIN_MATCH|" + playerId + "|" + matchId;
        sendMessage(msg);
        m_mainPlayerName = GameManager.Instance.MainPlayerName;
    }

    // Called automatically when GameManager leaves a match lobby
    private void leaveMatchLobbyHandler(string playerId, string matchId)
    {
        Debug.Log("GameManager asked to leave a match lobby");
        string msg = "LEAVE_MATCH_LOBBY|" + playerId + "|" + matchId;
        sendMessage(msg);
    }

    // Called automatically when GameManager loads the match
    private void gameLoadedHandler(string playerId, string matchId)
    {
        Vector3 spawnVector = new Vector3(0, 1, 0);
        List<string> playerIds = new List<string>(GameManager.Instance.Players.Keys);
        foreach (string player in playerIds)
        {
            GameManager.Instance.SpawnPlayer(player, spawnVector);
            spawnVector += new Vector3(3, 0, 0);
        }
    }

    // Called automatically when GameManager drops from a match
    private void dropMatchHandler(string playerId, string matchId)
    {
        Debug.Log("GameManager asked to drop a player");
        string msg = "DROP_PLAYER|" + playerId + "|" + matchId;
        sendMessage(msg);
        //Disconnect();
    }

    // Ask for server to send a list of open matches
    public void RequestOpenMatchList()
    {
        Debug.Log("Request Open Matches");
        string msg = "GET_OPEN_MATCHES";
        sendMessage(msg);
    }

    // Update list of open matches based on message msg from server
    private void UpdateOpenMatches(string msg)
    {
        Debug.Log("Updating open matches");
        m_openMatches.Clear();
        string[] splitData = msg.Split('|');
        for (int i = 1; i < splitData.Length; i++)
        {
            if(splitData[i] != "")
            {
                m_openMatches.Add(splitData[i]);
            }
        }
    }

    // Update players in match lobby based on message msg from server
    private void UpdatePlayerLobby(string msg)
    {
        string[] splitData = msg.Split('|');
        if (splitData[1] == GameManager.Instance.MatchName)
        {
            List<string> playerIds = new List<string>(GameManager.Instance.Players.Keys);
            foreach (string player in playerIds)
            {
                GameManager.Instance.RemovePlayer(player);
            }
            for (int i = 2; i < splitData.Length; i++)
            {
                GameManager.Instance.AddPlayer(splitData[i]);
            }
        }
    }

    // Send a request to start the match
    public void SendStartMatchRequest()
    {
        string msg = "START_MATCH|" + GameManager.Instance.MainPlayerName + "|" + GameManager.Instance.MatchName;
        sendMessage(msg);
    }

    // Start the match
    private void StartMatch(string matchName)
    {
        Debug.Log("Starting Match");
        if ((matchName == GameManager.Instance.MatchName) && !GameManager.Instance.InMatch)
        {
            GameManager.Instance.StartMatch(gameSceneName);
        }
    }

    // Returns a list of open matches
    public List<string> GetOpenMatches()
    {
        return m_openMatches;
    }

    // Move player
    private void MovePlayer(string playerId, string matchName, string position, string rotation)
    {
        if (matchName == GameManager.Instance.MatchName)
        {
            // Move Player
            position = position.Substring(1, position.Length - 2);
            string[] splitData = position.Split(',');
            Vector3 positionVec = new Vector3(float.Parse(splitData[0]), float.Parse(splitData[1]), float.Parse(splitData[2]));
            GameManager.Instance.MovePlayer(playerId, positionVec);

            // TEMP CODE = main player rotation currently done automatically and not based on server (see SendPlayerData)
            
            // Rotate player
            if (playerId != GameManager.Instance.MainPlayerName)
            {
                rotation = rotation.Substring(1, rotation.Length - 2);
                splitData = rotation.Split(',');
                Vector3 rotationVec = new Vector3(float.Parse(splitData[0]), float.Parse(splitData[1]), float.Parse(splitData[2]));
                GameManager.Instance.RotatePlayer(playerId, rotationVec);
            }          
        }
    }

    // Send player move data
    public void SendPlayerData()
    {
		if (m_players[m_mainPlayerName] != null)
        {
            PlayerMovement mainPlayerMovement = m_players[m_mainPlayerName].GetComponent<PlayerMovement>();
            Vector3 velocity = mainPlayerMovement.CalculateVelocity(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
            Vector3 rotation = mainPlayerMovement.CalculateHorizontalRotation(Input.GetAxis("Mouse X"));
            string msg = "MOVE_PLAYER|" + m_mainPlayerName + "|" + GameManager.Instance.MatchName + "|" + velocity.ToString() + "|" + rotation.ToString();
            sendMessage(msg);

            // TEMP CODE = main player rotation currently done automatically and not based on server
            GameManager.Instance.RotatePlayer(m_mainPlayerName, rotation);
        }
    }

    // Connect to server
    public void Connect()
    {
        Debug.Log("Trying to Connect");
        ConnectionConfig config = new ConnectionConfig();
        m_channelID = config.AddChannel(QosType.ReliableSequenced);
        HostTopology topology = new HostTopology(config, maxConnections);
        m_hostID = NetworkTransport.AddHost(topology, clientPort, null);
        Debug.Log("Socket open with HostID: " + m_hostID);
        m_connectionID = NetworkTransport.Connect(m_hostID, "127.0.0.1", serverPort, 0, out m_error);
    }

    // Disconnect from server
    public void Disconnect()
    {
        NetworkTransport.Disconnect(m_hostID, m_connectionID, out m_error);
    }

    // Send message to server
    public void sendMessage(string message)
    {
        byte[] buffer = Encoding.Unicode.GetBytes(message);
        if (m_hostID >= 0)
        {
            NetworkTransport.Send(m_hostID, m_connectionID, m_channelID, buffer, message.Length * sizeof(char), out m_error);
        }
    }
}
