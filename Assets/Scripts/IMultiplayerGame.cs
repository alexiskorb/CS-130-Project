using System.Collections.Generic;
using UnityEngine;

namespace Netcode {
	// @class PacketForClient
	// @desc IMultiplayerGame uses this for telling the netcode to send to a specific address.
	public struct PacketForClient {
		public ClientAddress m_clientAddr;
		public byte[] m_packet;
		public PacketForClient(ClientAddress clientAddr, byte[] packet)
		{
			m_clientAddr = clientAddr;
			m_packet = packet;
		}
	}

	// @class MultiplayerGame
	// @desc Games should implement this interface in order to be alerted to network events. 
	public abstract class IMultiplayerGame : MonoBehaviour {
		public abstract void NetEvent(Snapshot snapshot);
		public abstract void NetEvent(BulletSnapshot bulletSnapshot);
		public abstract void NetEvent(PlayerSnapshot playerSnapshot);
		public abstract void NetEvent(PlayerInput playerInput);
		public abstract void NetEvent(ClientAddress clientAddr, PacketType packetType, byte[] buf);

		// Bitmask for buttons pressed. InputBit is defined in Netcode.cs
		private InputBit inputBits_ = 0;
		// Server broadcasts the packets in this queue. Client sends them to the server.
		private Queue<byte[]> m_packetQueue = new Queue<byte[]>();
		// Queue for packets sent to specific clients.
		private Queue<PacketForClient> m_packetQueueForClient = new Queue<PacketForClient>();

		// @func QueuePacket
		// @desc Queue a packet for the netcode to send. 
		public void QueuePacket<T>(T packet) where T : Packet
		{
			byte[] buf = Serializer.Serialize(packet);
			m_packetQueue.Enqueue(buf);
		}

		// @func QueuePacket
		// @desc Queue a packet for the netcode to send to a specific client.
		public void QueuePacket<T>(ClientAddress clientAddr, T packet) where T : Packet
		{
			byte[] buf = Serializer.Serialize(packet);
			m_packetQueueForClient.Enqueue(new PacketForClient(clientAddr, buf));
		}

		// @func GetPacketsForClient
		// @desc Returns and clears all the packets in the queue.
		public Queue<PacketForClient> GetPacketsForClient()
		{
			Queue<PacketForClient> packetsForClient = new Queue<PacketForClient>(m_packetQueueForClient);
			m_packetQueueForClient.Clear();
			return packetsForClient;
		}

		// @func QueueInput
		// @desc Indicates that this button was pressed and should be sent in the PlayerInput packet.
		public void QueueInput(InputBit cmd)
		{
			inputBits_ |= cmd;
		}

		// @func GetInput
		// @desc Get the input bitmask and clear it for the next frame.
		public InputBit GetInput()
		{
			InputBit inputBits = inputBits_;
			inputBits_ &= 0;
			return inputBits;
		}

		// @func GetPacketQueue
		// @desc Clears the packet queue and returns the packets. Used by the client to
		// send game-related packets.
		public Queue<byte[]> GetPacketQueue()
		{
			Queue<byte[]> packetQueue = new Queue<byte[]>(m_packetQueue);
			m_packetQueue.Clear();
			return packetQueue;
		}

		// @func FindNetworkObjects
		// @desc Finds all game objects in the scene with type T. 
		public List<GameObject> FindNetworkObjects<T>() where T : MonoBehaviour
		{
			List<GameObject> networkObjects = new List<GameObject>();
			var gos = Resources.FindObjectsOfTypeAll<T>();

			foreach (var go in gos) {
				if (go.hideFlags == HideFlags.None && go.gameObject.scene.name != null) {
					networkObjects.Add(go.gameObject);
					continue;
				}
			}

			return networkObjects;
		}
	}
}