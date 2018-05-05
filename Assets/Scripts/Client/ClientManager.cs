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

    public string startingSceneName;

    public const int serverPort = 8889;
    public const int clientPort = 8888;

    private int m_connectionID;
    private int m_channelID;
    private int m_hostID;
    private int maxConnections = 4;
    private byte m_error;

    private string m_mainPlayerName;
    private Dictionary<string, GameObject> m_players;

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
        NetworkTransport.Init();

        // Load into main menu at start of game
        SceneManager.LoadScene(startingSceneName);

    }

    // Subscribe to createMatchEvent, startMatchEvent, dropMatchEvent
    private void OnEnable()
    {
        GameManager.createMatchEvent += createMatchHandler;
        GameManager.startMatchEvent += startMatchHandler;
        GameManager.dropMatchEvent += dropMatchHandler;
    }
    // Unsubscribe from createMatchEvent, startMatchEvent, dropMatchEvent
    private void OnDisable()
    {
        GameManager.createMatchEvent -= createMatchHandler;
        GameManager.startMatchEvent -= startMatchHandler;
        GameManager.dropMatchEvent -= dropMatchHandler;
    }

    // Called automatically when GameManager asks to create a match
    private void createMatchHandler(string playerId, string matchId)
    {
        Debug.Log("GameManager asked to create a match");
        Connect();
        string msg = "CREATE_MATCH|" + playerId + "|" + matchId;
        sendMessage(msg);
        m_mainPlayerName = GameManager.Instance.MainPlayerName;
    }

    // Called automatically when GameManager asks to start a match
    private void startMatchHandler(string playerId, string matchId)
    {
        Debug.Log("GameManager asked to start a match");
        GameManager.Instance.SpawnPlayer(GameManager.Instance.MainPlayerName, new Vector3(0,1,0));
        string msg = "START_MATCH|" + playerId + "|" + matchId;
        sendMessage(msg);
    }

    // Called automatically when GameManager asks to end a match
    private void dropMatchHandler(string playerId, string matchId)
    {
        Debug.Log("GameManager asked to drop a player");
        string msg = "DROP_PLAYER|" + playerId + "|" + matchId;
        sendMessage(msg);
        Disconnect();
    }

    // Update is called once per frame
    void Update()
    {
        int recHostID;
        int recConnectionID;
        int recChannelID;
        int bufferSize = 1024;
        byte[] recBuffer = new byte[bufferSize];
        int dataSize;
        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out m_error);

        m_players = GameManager.Instance.Players;

        if(GameManager.Instance.MatchStarted)
        {
            SendPlayerData();
        }
    }

    // Send player move data
    public void SendPlayerData()
    {
        if (m_players[m_mainPlayerName] != null)
        {
            string msg = "MOVE_PLAYER|" + m_mainPlayerName + "|" + m_players[m_mainPlayerName].transform.position.ToString() + "|" + GameManager.Instance.MatchName;
            sendMessage(msg);
            Debug.Log(msg);
        }
    }

    //This connect function is called from the UI element.
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

    public void Disconnect()
    {
        NetworkTransport.Disconnect(m_hostID, m_connectionID, out m_error);
    }


    public void sendMessage(string message)
    {
        byte[] buffer = Encoding.Unicode.GetBytes(message);
        if (m_hostID >= 0)
        {
            NetworkTransport.Send(m_hostID, m_connectionID, m_channelID, buffer, message.Length * sizeof(char), out m_error);
        }
    }
}
