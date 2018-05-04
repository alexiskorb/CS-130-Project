using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

//Written to prototype the LLAPI for Unet. Code is based on tutorials from Unity's documentation and
//a tutorial from Jonathon Merefield.

//Client code using the LLAPI. 
public class LLAPIClient : MonoBehaviour
{
    int connectionID;
    int channelID;
    int hostID;
    public const int serverPort = 8889;
    public const int clientPort = 8888;
    int maxConnections = 4;
    byte error;
    // Use this for initialization
    void Start()
    {
        NetworkTransport.Init();
        
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
        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

    }

    //This connect function is called from the UI element.
    public void Connect()
    {
        Debug.Log("Trying to Connect");
        ConnectionConfig config = new ConnectionConfig();
        channelID = config.AddChannel(QosType.ReliableSequenced);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, clientPort, null);
        Debug.Log("Socket open with HostID: " + hostID);
        connectionID = NetworkTransport.Connect(hostID, "127.0.0.1", serverPort, 0, out error);
    }

    public void Disconnect()
    {
        NetworkTransport.Disconnect(hostID, connectionID, out error);
    }

    //This is called by the Mvmt script based on input.
    public void sendMessage(string message)    
    {
        byte[] buffer = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostID, connectionID, channelID, buffer, message.Length * sizeof(char), out error);
    }
}
