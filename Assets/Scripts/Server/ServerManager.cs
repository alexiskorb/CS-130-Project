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
    public string startingSceneName;

    int m_connectionID;
    int m_channelID;
    int m_hostID;
    int socketPort = 8889;
    int maxConnections = 4;
    byte m_error;

    public Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();

    // Singleton instance of the ServerManager
    private static ServerManager m_instance = null;

    // Get instance of the ClientManager
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
        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int datasize;
        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out datasize, out m_error);

        //These cases signify if a new connection is established, a message from an existing client is received,
        //or a client disconnecting.
        switch (recNetworkEvent)
        {
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connected");
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, datasize);
                Debug.Log("Receiving: " + msg);
                string[] splitData = msg.Split('|');
                switch (splitData[0])
                {
                    case "START_MATCH":
                        StartMatch(splitData[1]);
                        break;
                    case "DROP_PLAYER":
                        DropPlayer(splitData[1]);
                        break;
                    case "MOVE_PLAYER":
                        MovePlayer(splitData[1], splitData[2]);
                        break;
                }
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnected");
                break;
        }
    }

    private void StartMatch(string playerId)
    {
        GameManager.Instance.AddPlayer(playerId);
        GameManager.Instance.SpawnPlayer(playerId, new Vector3(0, 1, 0));
    }
    private void DropPlayer(string playerId)
    {
        GameManager.Instance.RemovePlayer(playerId);
    }
    private void MovePlayer(string playerId, string position)
    {
        position = position.Substring(1, position.Length - 2);
        string[] splitData = position.Split(',');
        Vector3 positionVec = new Vector3(float.Parse(splitData[0]), float.Parse(splitData[1]), float.Parse(splitData[2]));
        GameManager.Instance.MovePlayer(playerId, positionVec);
    }

}
