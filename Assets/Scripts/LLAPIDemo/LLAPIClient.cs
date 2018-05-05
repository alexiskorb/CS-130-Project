using System.Collections;
using System.Net;
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
    public int clientPort;
    int maxConnections = 4;
	public Vector3 currentPlayerPos = new Vector3(0,0,0);
    byte error;
    // Use this for initialization
    void Start()
    {
		IPEndPoint ep = new IPEndPoint (IPAddress.Parse("127.0.0.1"), 0);
		clientPort = ep.Port;
		Debug.Log ("port is: " + clientPort);
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

		switch(recNetworkEvent)
		{
		case NetworkEventType.ConnectEvent:
			Debug.Log("Connected");
			break;
		case NetworkEventType.DataEvent:
			string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
			//Debug.Log("Receiving: " + msg);
			string[] splitData = msg.Split('|');
			switch(splitData[0])
			{
			//If you receive a packet with string starting with MV, call the move function on the attached game object.
			case "MV":
				if (splitData [1].StartsWith ("(") && splitData [1].EndsWith (")")) {
					splitData [1] = splitData [1].Substring (1, splitData.Length - 2);
				}

				// split the items
				string[] sArray = splitData [1].Split (',');

				// store as a Vector3
				Vector3 result = new Vector3 (
					                 float.Parse (sArray [0]),
					                 float.Parse (sArray [1]),
					                 float.Parse (sArray [2]));

				currentPlayerPos = result;
				Debug.Log (currentPlayerPos);
				break;
			}
			break;
		case NetworkEventType.DisconnectEvent:
			Debug.Log("Disconnected");
			break;
		}

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
