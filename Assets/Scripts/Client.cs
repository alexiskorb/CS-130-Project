using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using FpsNetcode;

// TODO:
// - Consider using Connect() instead of specifying the IP and port every time we send.

public class Client : MonoBehaviour {
	public const string SERVER_IP = "127.0.0.1";
	public const int SERVER_PORT = 9000;
	public GameObject playerPrefab;

	private UdpClient m_client;
	private GameObject m_mainPlayer;

	private PeriodicFunction m_sendPeriodic;

	void Start()
	{
		m_client = new UdpClient();
		m_client.BeginReceive(ReceiveCallback, m_client);
		m_mainPlayer = GameObject.Find("MainPlayer");
		m_sendPeriodic = new PeriodicFunction(SendPacket, 2.0f);
	}

	public void ClientLog(string message)
	{
		Debug.Log("<Client> " + message);
	}

	void SendVec3()
	{
		Vector3 vec3 = m_mainPlayer.transform.position;
		byte[] buf = new byte[3 * sizeof(float)];
		Buffer.BlockCopy(BitConverter.GetBytes(vec3.x), 0, buf, 0, sizeof(float));
		Buffer.BlockCopy(BitConverter.GetBytes(vec3.y), 0, buf, 1 * sizeof(float), sizeof(float));
		Buffer.BlockCopy(BitConverter.GetBytes(vec3.z), 0, buf, 2 * sizeof(float), sizeof(float));

		SendPacket(buf);
	}

	uint seqno = 0;
	Netcode.PacketType type = Netcode.PacketType.CONNECT;

	void SendPacket()
	{
		Netcode.CmdPacket cmdPacket = new Netcode.CmdPacket();
		cmdPacket.header.seqno = seqno++;
		cmdPacket.header.type = type++;
		SendPacket(Netcode.Serialize(cmdPacket));
	}

	// Send out updates (30ups?)
	void Update()
	{
		m_sendPeriodic.Run();
	}

	void ReceiveCallback(IAsyncResult asyncResult)
	{
		IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
		byte[] recvBuf = m_client.EndReceive(asyncResult, ref remoteEndPoint);
		m_client.BeginReceive(ReceiveCallback, m_client);

		Debug.Log("<Client> Message received from " + remoteEndPoint.Address.ToString() +
			" on port number " + remoteEndPoint.Port.ToString());
	}

	void SendPacket(byte[] dgram)
	{
		m_client.Send(dgram, dgram.Length, SERVER_IP, SERVER_PORT);
	}
}