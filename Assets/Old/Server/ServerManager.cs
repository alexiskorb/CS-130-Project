using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

//Written to prototype the LLAPI for Unet. Code is based on tutorials from Unity's documentation and a tutorial from Jonathon Merefield.

//Server code using the LLAPI. 
public class ServerManager : MonoBehaviour
{
    // Game scene to load into at start 
    public string startingSceneName;

    // Connection Information
    int messagesReceivedPerUpdate = 20;
    int m_connectionID;
    int m_channelID;
    int m_hostID;
    int socketPort = 8889;
    int maxConnections = 10;
    byte m_error;

    // Dictionary mapping open match names to a list of players in each match
    private Dictionary<string, List<string>> m_openMatches;

    // Contains information about a given client connection
    struct ConnectionInfo
    {
        public int hostId, connectionId, channelId;
        public ConnectionInfo(int hId, int coId, int chId)
        {
            hostId = hId;
            connectionId = coId;
            channelId = chId;
        }
    }
    // List of currently open connections
    private List<ConnectionInfo> m_openConnections;

    // Singleton instance of the ServerManager
    private static ServerManager m_instance = null;

    // Get instance of the ServerManager
    public static ServerManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<ServerManager>();
                if (m_instance == null)
                {
                    GameObject sm = new GameObject();
                    sm.name = "ServerManager";
                    m_instance = sm.AddComponent<ServerManager>();
                    DontDestroyOnLoad(sm);
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

    // Opens itself as a host
    void Start()
    {
        // Initialize data
        m_openMatches = new Dictionary<string, List<string>>();
        GameManager.Instance.InMatch = true;
        m_openConnections = new List<ConnectionInfo>();

        // Start up network
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        m_channelID = config.AddChannel(QosType.ReliableSequenced);
        HostTopology topology = new HostTopology(config, maxConnections);
        m_hostID = NetworkTransport.AddHost(topology, socketPort, null);
        Debug.Log("Socket open with HostID: " + m_hostID);

        // Load into game scene at start of game
        SceneManager.LoadScene(startingSceneName);
    }

    // Every frame it checks for connections
    void Update()
    {
        // Receive messages from clients
        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int datasize;

        // Receive and handle messagesReceivedPerUpdate number of messages
        for (int i = 0; i < messagesReceivedPerUpdate; i++ )
        {
            NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out datasize, out m_error);

            //These cases signify if a new connection is established, a message from an existing client is received, or a client is disconnecting.
            switch (recNetworkEvent)
            {
                case NetworkEventType.ConnectEvent:
                    string connectionMsg = "Connected " + recHostID + " " + recConnectionID + " " + recChannelID;
                    Debug.Log(connectionMsg);
                    // Add new connection to list of open connections
                    ConnectionInfo newConnection = new ConnectionInfo(recHostID, recConnectionID, recChannelID);
                    m_openConnections.Add(newConnection);
                    break;
                case NetworkEventType.DataEvent:
                    string msg = Encoding.Unicode.GetString(recBuffer, 0, datasize);
                    Debug.Log("Receiving: " + msg);
                    string[] splitData = msg.Split('|');
                    switch (splitData[0])
                    {
                        case "CREATE_MATCH":
                            CreateMatch(splitData[1], splitData[2]);
                            break;
                        case "JOIN_MATCH":
                            JoinMatch(splitData[1], splitData[2]);
                            break;
                        case "LEAVE_MATCH_LOBBY":
                            LeaveMatchLobby(splitData[1], splitData[2]);
                            break;
                        case "START_MATCH":
                            StartMatch(splitData[1], splitData[2]);
                            break;
                        case "DROP_PLAYER":
                            DropPlayer(splitData[1], splitData[2]);
                            break;
                        case "MOVE_PLAYER":
                            MovePlayer(splitData[1], splitData[2], splitData[3], splitData[4]);
                            break;
                        case "GET_OPEN_MATCHES":
                            GetOpenMatches(recHostID, recConnectionID, recChannelID);
                            break;
                    }
                    break;
                case NetworkEventType.DisconnectEvent:
                    Debug.Log("Disconnected");
                    // Remove connection from list of open connections
                    List<ConnectionInfo> currentConnections = m_openConnections;
                    foreach (ConnectionInfo connection in currentConnections)
                    {
                        if (connection.hostId == recHostID && connection.channelId == recChannelID && connection.connectionId == recConnectionID)
                        {
                            m_openConnections.Remove(connection);
                        }
                    }
                    break;
            }
        }
    }

    // Send a message to a given client
    public void sendMessage(string message, int hostId, int connectionId, int channelId)
    {
        byte[] buffer = Encoding.Unicode.GetBytes(message);
        if (m_hostID >= 0)
        {
            NetworkTransport.Send(hostId, connectionId, channelId, buffer, message.Length * sizeof(char), out m_error);
        }
    }

    // Send a message to all connections in m_openConnections
    private void SendMessageToAll(string message)
    {
        foreach(ConnectionInfo connection in m_openConnections)
        {
            sendMessage(message, connection.hostId, connection.connectionId, connection.channelId);
        }
    }
    
    // Create a match for a player
    // Currently, the most recently created match is the one the server is running.
    private void CreateMatch(string playerId, string matchName)
    {
        if(!m_openMatches.ContainsKey(matchName))
        {
            m_openMatches.Add(matchName, new List<string> { playerId });
        }
        GameManager.Instance.MatchName = matchName;
        GameManager.Instance.Players.Clear();
        GameManager.Instance.AddPlayer(playerId);
    }

    // Have a player join an existing match
    private void JoinMatch(string playerId, string matchName)
    {
        if (m_openMatches.ContainsKey(matchName))
        {
            m_openMatches[matchName].Add(playerId);
        }
        if (matchName == GameManager.Instance.MatchName)
        {
            GameManager.Instance.AddPlayer(playerId);
        }
        UpdatePlayerLobby(matchName);
    }

    // Have a player leave a match lobby
    private void LeaveMatchLobby(string playerId, string matchName)
    {
        if(m_openMatches.ContainsKey(matchName))
        {
            m_openMatches[matchName].Remove(playerId);
            if(m_openMatches[matchName].Count == 0)
            {
                m_openMatches.Remove(matchName);
            }
            else
            {
                UpdatePlayerLobby(matchName);
            }
        }
        if (matchName == GameManager.Instance.MatchName)
        {
            GameManager.Instance.RemovePlayer(playerId);
        }
    }

    // Start a match
    // Will only start the match if it is the most recently created match 
    private void StartMatch(string playerId, string matchName)
    {
        if (matchName == GameManager.Instance.MatchName)
        {
            Vector3 spawnVector = new Vector3(0, 1, 0);
            List<string> playerIds = new List<string>(GameManager.Instance.Players.Keys);
            foreach (string player in playerIds)
            {
                GameManager.Instance.SpawnPlayer(player, spawnVector);
                spawnVector += new Vector3(3, 0, 0);
            }
            string startMsg = "START_MATCH|" + GameManager.Instance.MatchName;
            SendMessageToAll(startMsg);
        }
    }

    // Drop a player from a match
    private void DropPlayer(string playerId, string matchName)
    {
        if (matchName == GameManager.Instance.MatchName)
        {
            GameManager.Instance.RemovePlayer(playerId);
        }
    }

    // Handles a MOVE_PLAYER request from client
    // Sets player velocity and rotation and messages all clients
    private void MovePlayer(string playerId, string matchName, string velocity, string rotation)
    {
        if (matchName == GameManager.Instance.MatchName)
        {
            // Move Player
            velocity = velocity.Substring(1, velocity.Length - 2);
            string[] splitData = velocity.Split(',');
            Vector3 velocityVec = new Vector3(float.Parse(splitData[0]), float.Parse(splitData[1]), float.Parse(splitData[2]));
            GameManager.Instance.SetPlayerVelocity(playerId, velocityVec);
          
            // Rotate player
            rotation = rotation.Substring(1, rotation.Length - 2);
            splitData = rotation.Split(',');
            Vector3 rotationVec = new Vector3(float.Parse(splitData[0]), float.Parse(splitData[1]), float.Parse(splitData[2]));
            GameManager.Instance.RotatePlayer(playerId, rotationVec);

            // Get player position
            Vector3 position = GameManager.Instance.Players[playerId].transform.position;

            string msg = "MOVE_PLAYER|" + playerId + "|" + GameManager.Instance.MatchName + "|" + position.ToString() + "|" + rotationVec.ToString();
            SendMessageToAll(msg);
        }
    }

    // Send a list of open matches to the connection specified
    private void GetOpenMatches(int hostId, int connectionId, int channelId)
    {
        string msg = "OPEN_MATCH_LIST";
        foreach (string matchId in m_openMatches.Keys)
        {
            msg += "|" + matchId;
        }
        sendMessage(msg, hostId, connectionId, channelId);
    }

    // Send a player lobby update to all connections
    private void UpdatePlayerLobby(string matchId)
    {
        string msg = "PLAYER_LOBBY_UPDATE|" + matchId;
        foreach (string playerId in m_openMatches[matchId])
        {
            msg += "|" + playerId;
        }
        SendMessageToAll(msg);
    }
}
