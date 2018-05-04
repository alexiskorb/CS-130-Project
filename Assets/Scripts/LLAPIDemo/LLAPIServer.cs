using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

//Written to prototype the LLAPI for Unet. Code is based on tutorials from Unity's documentation and
//a tutorial from Jonathon Merefield.

//Server code using the LLAPI. 
public class LLAPIServer : MonoBehaviour {
    int connectionID;
    int channelID;
    int hostID;
    int socketPort = 8889;
    int maxConnections = 4;
    byte error;

    public GameObject playerObject;
    public Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();

	// Opens itself as a host
	void Start () {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        channelID = config.AddChannel(QosType.ReliableSequenced);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);
        Debug.Log("Socket open with HostID: " + hostID);
	}


	
	// Every frame it checks for connections
	void Update () {
        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int datasize;
        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out datasize, out error);
        
        //These cases signify if a new connection is established, a message from an existing client is received,
        //or a client disconnecting.
        switch(recNetworkEvent)
        {
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connected");
                //If a client connects, create the specified object.
                GameObject temp = Instantiate(playerObject, transform.position, transform.rotation);
                players.Add(recConnectionID, temp);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, datasize);
                Debug.Log("Receiving: " + msg);
                string[] splitData = msg.Split('|');
                switch(splitData[0])
                {
                    //If you receive a packet with string starting with MV, call the move function on the attached game object.
                    case "MV":
                        Move(splitData[1], splitData[2], players[recConnectionID]);
                        break;
                }
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnected");
                break;
        }
	}

    void Move(string x, string y, GameObject obj)
    {
        float xMov = float.Parse(x);
        float yMov = float.Parse(y);
        obj.transform.Translate(xMov, 0f, yMov);
    }
}
