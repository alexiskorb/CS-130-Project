﻿using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

namespace Netcode {
	// @class MultiplayerNetworking
	// @desc Contains code shared by the client and server. 
	public abstract class MultiplayerNetworking : MonoBehaviour {
		public delegate void MainThreadWork();
		public delegate void PacketHandler(ClientAddress clientAddr, byte[] buf);

		public Queue<MainThreadWork> m_mainWork = new Queue<MainThreadWork>();
		private UdpClient m_udp;
		private TcpClient m_tcp; // TODO: TpcClient is bad. Use the C# socket library for sending reliable messages. 
		private Dictionary<PacketType, PacketHandler> m_packetCallbacks = new Dictionary<PacketType, PacketHandler>();
		private IMultiplayerGame m_multiplayerGame;

        private string MASTER_SERVER_IP = "";
        private int MASTER_SERVER_PORT = 0;
        public ClientAddress MasterServer;

        public void InitNetworking(IMultiplayerGame game, string masterServerIp, int masterServerPort, int portno = 0)
		{
			m_multiplayerGame = game;
            MASTER_SERVER_IP = masterServerIp;
            MASTER_SERVER_PORT = masterServerPort;
            MasterServer = new ClientAddress(MASTER_SERVER_IP, MASTER_SERVER_PORT);
            Debug.Log(MasterServer.m_ipAddress);
            Debug.Log(MasterServer.m_port);
            m_udp = new UdpClient(portno);
			m_udp.BeginReceive(ReceiveCallback, m_udp);
		}

		public void ProcessPacketsInQueue()
		{
			// Process the packets in the incoming queue.
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
            if (clientAddr.m_port == MasterServer.m_port && clientAddr.m_ipAddress == MasterServer.m_ipAddress)
            {
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
			} else {
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
            Debug.Log(addr.m_ipAddress);
            Debug.Log(addr.m_port);
			m_udp.Send(buf, buf.Length, addr.m_ipAddress, addr.m_port);
		}



		// @func ShouldDiscard
		// @desc Decides how packets are dropped. 
		public abstract bool ShouldDiscard(ClientAddress clientAddr, Packet header);
	}
}
