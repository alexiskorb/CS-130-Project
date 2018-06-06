using System.Collections.Generic;
using UnityEngine;

namespace Netcode {
    // @class PacketForClient
    // @desc IMultiplayerGame uses this for telling the netcode to send to a specific address.

    public struct PacketForClient
    {
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
    public abstract class IMultiplayerGame : MonoBehaviour
    {
        public abstract void NetEvent(Snapshot snapshot);
        public abstract void NetEvent(BulletSnapshot bulletSnapshot);
        public abstract void NetEvent(PlayerSnapshot playerSnapshot);
        public abstract void NetEvent(PlayerInput playerInput);
        public abstract void NetEvent(ClientAddress clientAddr, PacketType packetType, byte[] buf);
        public abstract void MasterServerEvent(byte[] buf);

        private InputBit inputBits_ = 0; // Bitmask for buttons pressed. 
        private Queue<byte[]> m_packetQueue = new Queue<byte[]>();
        private Queue<PacketForClient> m_packetQueueForClient = new Queue<PacketForClient>();

        private Dictionary<string, PacketForClient> m_reliablePackets = new Dictionary<string, PacketForClient>();

        // @func QueuePacket
        // @desc Queue a packet for the client to send. 
        public void QueuePacket<T>(T packet) where T : Packet
        {
            byte[] buf = Serializer.Serialize(packet);
            m_packetQueue.Enqueue(buf);
        }

        public void QueuePacket<T>(ClientAddress clientAddr, T packet) where T : Packet
        {
            byte[] buf = Serializer.Serialize(packet);
            m_packetQueueForClient.Enqueue(new PacketForClient(clientAddr, buf));
        }
        public void QueuePacket(ClientAddress clientAddr, string message)
        {
            byte[] buf = System.Text.Encoding.UTF8.GetBytes(message);
            m_packetQueueForClient.Enqueue(new PacketForClient(clientAddr, buf));
        }
        public void QueuePacket(ClientAddress clientAddr, byte[] message)
        {
            m_packetQueueForClient.Enqueue(new PacketForClient(clientAddr, message));
        }
        public void AddReliablePacket(string key, ClientAddress clientAddr, string message)
        {
            byte[] buf = System.Text.Encoding.UTF8.GetBytes(message);
            Debug.Log("Adding " + message + " to ReliablePackets");
            
            PacketForClient reliablePacket = new PacketForClient(clientAddr, buf);
            m_reliablePackets[key] = reliablePacket; 
        }
        public void AddReliablePacket<T>(string key, ClientAddress clientAddr, T packet) where T: Packet 
        {
            byte[] buf = Serializer.Serialize(packet);
            PacketForClient reliablePacket = new PacketForClient(clientAddr, buf);
            m_reliablePackets[key] = reliablePacket;
        }

        public void RemoveReliablePacket(string key)
        {
            
            string com = key.Substring(0, 5);
            Debug.Log("Removing " + com + "from ReliablePackets");
            m_reliablePackets.Remove(key);
            
        }
        public bool WaitingForAck(string key)
        {
            
            if (m_reliablePackets.ContainsKey(key))
                return true;
            else
                return false;
        }

        public Queue<PacketForClient> GetPacketsForClient()
        {
            Queue<PacketForClient> packetsForClient = new Queue<PacketForClient>(m_packetQueueForClient);
            m_packetQueueForClient.Clear();
            return packetsForClient;
        }

        public void QueueInput(InputBit cmd)
        {
            inputBits_ |= cmd;
        }

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
        public Dictionary<string, PacketForClient> GetReliablePackets()
        {
            return m_reliablePackets;
        }


        // @func FindNetworkObjects
        // @desc Finds all game objects in the scene with type T. 
        public List<GameObject> FindNetworkObjects<T>() where T : MonoBehaviour
        {
            List<GameObject> networkObjects = new List<GameObject>();
            var gos = Resources.FindObjectsOfTypeAll<T>();

            foreach (var go in gos)
            {
                if (go.hideFlags == HideFlags.None)
                {
                    networkObjects.Add(go.gameObject);
                    continue;
                }
            }

            return networkObjects;
        }
    }
}
