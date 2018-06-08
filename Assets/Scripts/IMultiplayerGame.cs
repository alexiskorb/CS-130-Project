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

        //A dictionary that holds packets that are sent periodically to ensure reliable delivery. When using the dictionary,
        //select a string key that will uniquely identify the packet and its ack, so that when the ack is received, remove the
        //dictionary entry with the key.
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
        //Add to dictionary of packets to be sent reliably. Packets included will be sent periodically.
        //Select a unique key string so that the packet can be identified when the ack is received.
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
            Debug.Log("Adding " + packet.m_type.ToString() + " to ReliablePackets");
            PacketForClient reliablePacket = new PacketForClient(clientAddr, buf);
            m_reliablePackets[key] = reliablePacket;
        }
        //Remove the reliable packet using a key. The packet will not be sent periodically after the function.
        public void RemoveReliablePacket(string key)
        {
            
            string com = key.Substring(0, 5);
            Debug.Log("Removing " + com + "from ReliablePackets");
            m_reliablePackets.Remove(key);
            
        }
        // @func WaitingForAck
        // @desc Checks if there is a packet that is being sent reliably based on the key. This is used to process the ack once,
        // and ignore all duplicate acks received later.
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
        // @func GetReliablePackets
        // @desc Return all packets that require reliable transmission.
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
                if (go.hideFlags == HideFlags.None && go.gameObject.scene.name != null)
                {
                    networkObjects.Add(go.gameObject);
                    continue;
                }
            }

            return networkObjects;
        }
    }
}
