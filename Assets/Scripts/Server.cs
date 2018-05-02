using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using FpsNetcode;

public struct ClientAddress {
	public string ipAddress;
	public int port;
	// TODO: A unique, server-assigned client ID field would be useful for when 
	// a client's connection moves. (Salted GetHashCode?)
	public ClientAddress(string ipAddress, int port)
	{
		this.ipAddress = ipAddress;
		this.port = port;
	}
}

public struct ConnectionInfo {
	public uint seqno;
	public float timeSinceLastAck;
	public ConnectionInfo(uint seqno, float timeSinceLastAck)
	{
		this.seqno = seqno;
		this.timeSinceLastAck = timeSinceLastAck;
	}
}

public struct PlayerInfo {

}

public struct ClientState {
	public ConnectionInfo connection;
	public PlayerInfo player;
	public ClientState(ConnectionInfo connection, PlayerInfo player)
	{
		this.connection = connection;
		this.player = player;
	}
}

// The authoritative server. 
public class Server : MonoBehaviour {
	private const int SERVER_PORT = 9000;
	private UdpClient m_server;
	private Dictionary<ClientAddress, ClientState> m_connections;	// <ClientAddress, ConnectionInfo>

	public void ReceiveCallback(IAsyncResult asyncResult)
	{
		IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
		byte[] buf = m_server.EndReceive(asyncResult, ref remoteEndPoint);
		m_server.BeginReceive(ReceiveCallback, m_server);

		ServerLog("Message from " + remoteEndPoint.Address.ToString() + 
			" on port " + remoteEndPoint.Port.ToString());

		ProcessMessage(buf, remoteEndPoint);
	}

	public void ServerLog(string message)
	{
		Debug.Log("<Server> " + message);
	}

	public void ProcessMessage(byte[] buf, IPEndPoint endpoint)
	{
		Netcode.PacketHeader header = Netcode.GetHeader(buf);
		ClientAddress clientInfo = new ClientAddress(endpoint.Address.ToString(), endpoint.Port);

		// If it's a new client, make sure it's a CONNECT packet.
		if (!m_connections.ContainsKey(clientInfo) && header.type != Netcode.PacketType.CONNECT) {
			ServerLog("Client must establish a connection before sending.\nIP: " + clientInfo.ipAddress + " Port: " + clientInfo.port);
			return;
		}

		if (header.type == Netcode.PacketType.CONNECT) {
			ServerLog("Client connected! Seqno " + header.seqno);
			ProcessConnect(clientInfo);
			return;
		}

		// Check the seqno.
		ClientState state = m_connections[clientInfo];
		if (header.seqno < state.connection.seqno) {
			ServerLog("Packet received out of order.");
			return;
		}

		switch (header.type) {
			case Netcode.PacketType.CLIENT_SNAPSHOT:
				ServerLog("Client snapshot received! Seqno " + header.seqno);
				break;
			case Netcode.PacketType.CLIENT_CMD:
				ServerLog("Client sent a command! Seqno " + header.seqno);
				break;
			default:
				ServerLog("Dunno.");
				break;
		}
	}

	public void ProcessConnect(ClientAddress clientInfo)
	{
		// If this is a new client, add it to the client connection table. 
		if (!m_connections.ContainsKey(clientInfo)) {
			ConnectionInfo connection = new ConnectionInfo(0, 0f);
			PlayerInfo player = new PlayerInfo();
			ClientState state = new ClientState(connection, player);
			m_connections.Add(clientInfo, state);
		}
		// Else, discard the packet. 
	}

	void Start()
	{
		m_server = new UdpClient(SERVER_PORT);
		m_server.BeginReceive(ReceiveCallback, m_server);
		m_connections = new Dictionary<ClientAddress, ClientState>();
	}

	// For each client: 
	// - If timeSinceLastAck >= clientTimeoutPeriod, disconnect client. 
	// - Send client update, incrementing seqnos. 
	// - Update timeSinceLastAck
	void Update()
	{
	}

	void OnDestroy()
	{
		m_server.Close();
	}
}