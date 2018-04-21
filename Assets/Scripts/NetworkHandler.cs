using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Singleton Network Manager
public class NetworkHandler : MonoBehaviour {

    private static NetworkHandler m_instance = null;

    // Network variables
    private int m_channelId;
    private int m_hostId;
    private int m_hostPort;
    private string m_destAddr;
    private int m_destPort;
    private int m_connectionId;

    // Get instance of the NetworkHandler
    public static NetworkHandler Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<NetworkHandler>();
                if (m_instance == null)
                {
                    GameObject nh = new GameObject();
                    nh.name = "NetworkHandler";
                    m_instance = nh.AddComponent<NetworkHandler>();
                    DontDestroyOnLoad(nh);
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

    void Start()
    {
        // Set up Network
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        m_channelId = config.AddChannel(QosType.Unreliable);
        int maxConnections = 10;
        HostTopology topology = new HostTopology(config, maxConnections);
        m_hostPort = 8888;
        m_hostId = NetworkTransport.AddHost(topology, m_hostPort);
        m_destAddr = "";
        m_destPort = 8888;
    }

    void Update()
    {
        // Receive Network Message
        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData)
        {
            // Nothing
            case NetworkEventType.Nothing:
                break;
            // Connection
            case NetworkEventType.ConnectEvent:
                break;
            // Message Received
            case NetworkEventType.DataEvent:
                break;
            // Disconnection
            case NetworkEventType.DisconnectEvent:
                break;
        }
    }

    // Send connect request to server
    public void ConnectToServer()
    {
        byte error;
        m_connectionId = NetworkTransport.Connect(m_hostId, m_destAddr, m_destPort, 0, out error);
    }

    // Send disconnect request to server
    public void DisconnectFromServer()
    {
        byte error;
        NetworkTransport.Disconnect(m_hostId, m_connectionId, out error);
    }

    // Send message to server
    public void SendMessage(byte[] buffer, int bufferSize)
    {
        byte error;
        NetworkTransport.Send(m_hostId, m_connectionId, m_channelId, buffer, bufferSize, out error);
    }
}
