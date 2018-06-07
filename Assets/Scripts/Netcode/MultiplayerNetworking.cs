using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

namespace Netcode {
	// @class MultiplayerNetworking
	// @desc Contains code shared by the client and server. 
	public abstract class MultiplayerNetworking : MonoBehaviour {
		// Packet handlers take this form. clientAddr is the address of the sender, 
		// buf is the bytes received.
		public delegate void PacketHandler(ClientAddress clientAddr, byte[] buf);

		// The work queue.
		private Queue<MainThreadWork> m_mainWork = new Queue<MainThreadWork>();
		// UdpClient class for sending
		private UdpClient m_udp;
		// If a reliable layer is ever added to the netcode, it's probably best to use the C# socket library. 
		private TcpClient m_tcp; 
		// Contains the packet callbacks for each packet type.
		private Dictionary<PacketType, PacketHandler> m_packetCallbacks = new Dictionary<PacketType, PacketHandler>();
		// Reference to the Multiplayer Game interface so the netcode can pass messages up to the game. 
		private IMultiplayerGame m_multiplayerGame;

        private string MASTER_SERVER_IP = "";
        private int MASTER_SERVER_PORT = 0;
        public ClientAddress MasterServer;
        // @func InitNetworking
        // @desc Initialize the netcode with port number, and begin listening for UDP packets. 
        public void InitNetworking(IMultiplayerGame game, string masterServerIp, int masterServerPort, int portno = 0)
		{
			m_multiplayerGame = game;
            MASTER_SERVER_IP = masterServerIp;
            MASTER_SERVER_PORT = masterServerPort;
            MasterServer = new ClientAddress(MASTER_SERVER_IP, MASTER_SERVER_PORT);
            Debug.Log(MasterServer.m_ipAddress);
            Debug.Log(MasterServer.m_port);
            m_udp = new UdpClient();
			m_udp.BeginReceive(ReceiveCallback, m_udp);
		}

		// @func ProcessPacketsInQueue
		// @desc Calls the packet handlers that were set by the asynchronous read callback. 
		public void ProcessPacketsInQueue()
		{
			while (m_mainWork.Count > 0) {
				MainThreadWork work = m_mainWork.Dequeue();
				if (work != null)
					work.Invoke();
			}
		}

		// @func ReceiveCallback
		// @desc Asynchronous callback for receiving packets.
		public void ReceiveCallback(IAsyncResult asyncResult)
		{
			IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
			byte[] buf = m_udp.EndReceive(asyncResult, ref remoteEndPoint);
			m_udp.BeginReceive(ReceiveCallback, m_udp);
			ClientAddress clientAddr = new ClientAddress(remoteEndPoint.Address.ToString(), remoteEndPoint.Port);
           
            MainThreadWork work = () => {
                HandlePacket(clientAddr, buf);
			};

			m_mainWork.Enqueue(work);
        }

        // @func RegisterPacket
        // @desc Associates the packet handler with this packet type. 
        public void RegisterPacket(PacketType packetType, PacketHandler packetHandler)
		{
			m_packetCallbacks[packetType] = packetHandler;
		}

		// @func HandlePacket
		// @desc Called every time a packet is received. HandlePacket will call
		// the appropriate packet handler. 
		public void HandlePacket(ClientAddress clientAddr, byte[] buf)
		{
            Debug.Log("Received Packet from " + clientAddr.m_ipAddress + ":" + clientAddr.m_port);
            bool masterEvent = false;
            if (clientAddr.m_port == MasterServer.m_port && clientAddr.m_ipAddress == MasterServer.m_ipAddress)
            {
                masterEvent = true;
                m_multiplayerGame.MasterServerEvent(buf);
            }
            Packet header;
			try {
				header = Serializer.Deserialize<Packet>(buf);
			} catch (Exception) {
				Debug.Log("Packet deserialization failed.");
				return;
			}

			if (ShouldDiscard(clientAddr, header))
				return;

            //Debug.Log("Packet type is" + header.m_type);
			if (m_packetCallbacks.ContainsKey(header.m_type)) {
				m_packetCallbacks[header.m_type].Invoke(clientAddr, buf);
			}
            else if (!masterEvent) { 
				m_multiplayerGame.NetEvent(clientAddr, header.m_type, buf);
			}
		}

		// @func RemovePacketHandler
		// @desc Stops using this callback for the given packet type. 
		public void RemovePacketHandler(PacketType packetType)
		{
			m_packetCallbacks.Remove(packetType);
		}

		// @func SendPacket
		// @desc Sends packets without the caller having to serialize.
		public void SendPacket<T>(ClientAddress addr, T packet) where T : Packet
		{
			byte[] buf = Serializer.Serialize(packet);
			SendPacket(addr, buf);
		}

		// @func SendPacket
		// @desc Sends the packet to the specified address. 
		public void SendPacket(ClientAddress addr, byte[] buf)
		{
			m_udp.Send(buf, buf.Length, addr.m_ipAddress, addr.m_port);
		}



		// @func ShouldDiscard
		// @desc Decides how packets are dropped. 
		public abstract bool ShouldDiscard(ClientAddress clientAddr, Packet header);
	}
}
