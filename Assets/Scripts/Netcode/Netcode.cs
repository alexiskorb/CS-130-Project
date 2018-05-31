using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

// @namespace Netcode
// @desc Netcode contains the packet data structures used by the client and server. 
// All packets must have a default constructor in order for serialization to work. 
namespace Netcode {
	public delegate void MainThreadWork();

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
    public abstract class IMultiplayerGame : MonoBehaviour {
		public abstract void NetEvent(Snapshot snapshot);
		public abstract void NetEvent(BulletSnapshot bulletSnapshot);
        public abstract void NetEvent(PlayerSnapshot playerSnapshot);
        public abstract void NetEvent(PlayerInput playerInput);
		public abstract void NetEvent(ClientAddress clientAddr, PacketType packetType, byte[] buf);

		private InputBit inputBits_ = 0; // Bitmask for buttons pressed. 
		private Queue<byte[]> m_packetQueue = new Queue<byte[]>();
        private Queue<PacketForClient> m_packetQueueForClient = new Queue<PacketForClient>();

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

		// @func FindNetworkObjects
		// @desc Finds all game objects in the scene with type T. 
		public List<GameObject> FindNetworkObjects<T>() where T : MonoBehaviour
		{
			List<GameObject> networkObjects = new List<GameObject>();
			var gos = Resources.FindObjectsOfTypeAll<T>();

			foreach (var go in gos) {
				if (go.hideFlags == HideFlags.None) {
					networkObjects.Add(go.gameObject);
					continue;
				}
			}

			return networkObjects;
		}
	}

	public enum PacketType : int {
		CREATE_LOBBY,
		REFRESH_LOBBY_LIST,
		JOIN_LOBBY,
        LEAVE_LOBBY,
		START_GAME,
		SNAPSHOT,
		BULLET_SNAPSHOT,
        PLAYER_SNAPSHOT,
		PLAYER_INPUT,
		INVITE_PLAYER
	}

	public enum InputBit : int {
		BEGIN = 0,
		PRIMARY_WEAPON = 1 << 0,
		END
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class BulletSnapshot : ISnapshot<BulletSnapshot> {
		public Vector3 position_;
		public Vector3 eulerAngles_;

		public BulletSnapshot()
		{
			m_type = PacketType.BULLET_SNAPSHOT;
		}

		public BulletSnapshot(uint seqno, int serverId, int serverHash, GameObject gameObject)
			: base(serverId, PacketType.BULLET_SNAPSHOT, seqno, gameObject) { }

		public override void Apply(ref GameObject gameObject)
		{
			gameObject.transform.position = position_;
			gameObject.transform.eulerAngles = eulerAngles_;
		}

		public override void FromObject(GameObject gameObject)
		{
			position_ = gameObject.transform.position;
			eulerAngles_ = gameObject.transform.eulerAngles;
		}

		public override bool Equals(BulletSnapshot other)
		{
			return (position_ == other.position_) &&
				(eulerAngles_ == other.eulerAngles_);
		}
	}

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class PlayerSnapshot : ISnapshot<PlayerSnapshot>
    {
        public int m_currentLife;

        public PlayerSnapshot()
        {
            m_type = PacketType.PLAYER_SNAPSHOT;
        }

        public PlayerSnapshot(uint seqno, int serverId, int serverHash, GameObject gameObject)
            : base(serverId, PacketType.PLAYER_SNAPSHOT, seqno, gameObject) { }

        public override void Apply(ref GameObject gameObject)
        {
            gameObject.GetComponentInChildren<NetworkedPlayer>().CurrentLife = m_currentLife;
        }

        public override void FromObject(GameObject gameObject)
        {
            m_currentLife = gameObject.GetComponentInChildren<NetworkedPlayer>().CurrentLife;
        }

        public override bool Equals(PlayerSnapshot other)
        {
            return (m_currentLife == other.m_currentLife);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class PlayerInput : Packet {
		public int serverId_;
		public uint seqno_;
		public InputBit cmdBits_;
		public PlayerInput() { }
		public PlayerInput(uint seqno, int serverId, InputBit cmdBits)
			: base(PacketType.PLAYER_INPUT)
		{
			seqno_ = seqno;
			serverId_ = serverId;
			cmdBits_ = cmdBits;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class CreateLobby : Packet {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string m_lobbyName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string m_hostPlayerName;
		public CreateLobby() : base(PacketType.CREATE_LOBBY) { }
		public CreateLobby(string lobbyname, string hostPlayerName)
			: base(PacketType.CREATE_LOBBY)
		{
			m_lobbyName = lobbyname;
			m_hostPlayerName = hostPlayerName;
		}
	}

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class RefreshLobbyList : Packet {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string m_listOfGames;
		public RefreshLobbyList() : base(PacketType.REFRESH_LOBBY_LIST) { }
		public RefreshLobbyList(string[] listOfGames)
			: base(PacketType.REFRESH_LOBBY_LIST)
		{
			m_listOfGames = Serializer.Serialize(listOfGames);
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class JoinLobby : Packet {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 140)]
        public string m_listOfPlayers;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string m_lobbyName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string m_playerName;
		public JoinLobby() : base(PacketType.JOIN_LOBBY) { }
		public JoinLobby(string[] playerList, string lobbyName, string playerName)
			: base(PacketType.JOIN_LOBBY)
		{
            m_listOfPlayers = Serializer.Serialize(playerList);
            m_lobbyName = lobbyName;
            m_playerName = playerName;
		}
	}

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class LeaveLobby : Packet
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 140)]
        public string m_listOfPlayers;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string m_lobbyName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string m_playerName;
        public LeaveLobby() : base(PacketType.LEAVE_LOBBY) { }
        public LeaveLobby(string[] playerList, string lobbyName, string playerName)
            : base(PacketType.LEAVE_LOBBY)
        {
            m_listOfPlayers = Serializer.Serialize(playerList);
            m_lobbyName = lobbyName;
            m_playerName = playerName;
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class StartGame : Packet {
		public int m_serverId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string m_hostIP;
        public int m_hostPort;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string m_matchName;
        public StartGame() : base(PacketType.START_GAME) { }
		public StartGame(string lobbyName)
            : base(PacketType.START_GAME)
        {
            m_matchName = lobbyName;
        }
		public StartGame(string lobbyName, int serverId, string hostIP, int hostPort)
			: base(PacketType.START_GAME)
		{
            m_matchName = lobbyName;
			m_serverId = serverId;
            m_hostIP = hostIP;
            m_hostPort = hostPort;
		}
	}


	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class InvitePlayer : Packet {
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
		public string m_lobbyName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
		public string m_hostSteamName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
		public string m_invitedSteamName;
		public InvitePlayer() : base(PacketType.INVITE_PLAYER) { }
		public InvitePlayer(string lobbyname, string hostSteamName, string invitedSteamName)
			: base(PacketType.INVITE_PLAYER)
		{
			m_lobbyName = lobbyname;
			m_hostSteamName = hostSteamName;
			m_invitedSteamName = invitedSteamName;
		}
	}

	// @doc A Snapshot is the state that is synchronized among clients and server.
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class Snapshot : ISnapshot<Snapshot> {
		public Vector3 m_position;
		public Vector3 m_eulerAngles;
        public Vector3 m_childEulerAngles;
        public int m_serverHash;

		public Snapshot() { }
		// @doc Constructor must take this form or you'll get compiler errors. 
		// Static errors are better than runtime errors :)  
		public Snapshot(uint seqno, int serverId, int serverHash, GameObject gameObject)
			: base(serverId, PacketType.SNAPSHOT, seqno, gameObject)
		{
			m_serverHash = serverHash;
		}

		public override void Apply(ref GameObject gameObject)
		{
			gameObject.transform.position = m_position;
			gameObject.transform.eulerAngles = m_eulerAngles;
            if(gameObject.transform.GetChild(0) != null)
            {
                gameObject.transform.GetChild(0).eulerAngles = m_childEulerAngles;
            }
        }

		public override void FromObject(GameObject gameObject)
		{
			m_position = gameObject.transform.position;
			m_eulerAngles = gameObject.transform.eulerAngles;
            if (gameObject.transform.GetChild(0) != null)
            {
                m_childEulerAngles = gameObject.transform.GetChild(0).eulerAngles;
            }
        }

		public override bool Equals(Snapshot other)
		{
            return (m_position == other.m_position) &&
                (m_eulerAngles == other.m_eulerAngles) &&
                (m_childEulerAngles== other.m_childEulerAngles);
		}
	}

	// @class SnapshotInterface
	// @desc Implement this interface to have snapshots integrated with the rest of the server.
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public abstract class ISnapshot<T> : Packet {
		public int m_serverId;
		public uint m_seqno;

		public ISnapshot() { }
		public ISnapshot(int serverId, PacketType type, uint seqno, GameObject gameObject)
			: base(type)
		{
			m_seqno = seqno;
			m_serverId = serverId;
			FromObject(gameObject);
		}

		public void Create(uint seqno, int serverId, GameObject gameObject)
		{
			m_seqno = seqno;
			m_serverId = serverId;
			FromObject(gameObject);
		}

		// @interface Equals
		// @desc Performs an equality test. 
		public abstract bool Equals(T other);
		// @interface FromObject
		// @desc Initialize the snapshot with a Game Object.
		public abstract void FromObject(GameObject gameObject);
		// @interface Apply
		// @desc Applies the snapshot to the Game Object.
		public abstract void Apply(ref GameObject gameObject);
	}

	// *****************************************************************************************
	//		@doc Everything after this is game-independent and shouldn't really be touched 
	// *****************************************************************************************

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class Packet {
		public PacketType m_type;

		public Packet() { }
		public Packet(PacketType type)
		{
			m_type = type;
		}
	}

	public struct ClientAddress {
		public string m_ipAddress;
		public int m_port;

		public ClientAddress(string ipAddress, int port)
		{
			m_ipAddress = ipAddress;
			m_port = port;
		}

		public String Print()
		{
			return "IP: " + m_ipAddress + " Port: " + m_port;
		}
	}

	// @class PeriodicFunction
	// @desc Executes a function every N seconds. Particularly useful for network debugging...
	public class PeriodicFunction {
		public delegate void DoEveryNSeconds();

		private DoEveryNSeconds m_doEveryN;
		private float m_timeRemaining;
		private float m_period;

		public PeriodicFunction(DoEveryNSeconds doFunc, float period)
		{
			m_doEveryN = doFunc;
			m_period = period;
			m_timeRemaining = period;
		}

		public void Run()
		{
			if (m_timeRemaining <= 0) {
				m_doEveryN();
				m_timeRemaining = m_period;
			}

			m_timeRemaining -= Time.deltaTime;
		}

		public void RunNow()
		{
			m_doEveryN();
		}
	}

	// @source http://thecodeisart.blogspot.com/2008/11/with-this-class-you-can-easy-convert.html
	// (With some modifications)
	public static class Serializer {
		public static byte[] Serialize<T>(T obj)
		{
			byte[] buf = Malloc(obj);
			IntPtr ptr = Marshal.AllocHGlobal(buf.Length);
			Marshal.StructureToPtr(obj, ptr, false);
			Marshal.Copy(ptr, buf, 0, buf.Length);
			Marshal.FreeHGlobal(ptr);
			return buf;
		}

        // Serialization of arrays in a class causes issues. For string arrays, it is necessary
        // to manually serialize the string, and this is called by the constructor when a class
        // has a string[] parameter in its constructor
        //TODO: Extend serialize so that it will automatically serialize all array types
        public static string Serialize(string[] stringArray)
        {
            if(stringArray != null && stringArray.Length > 0)
            {
                string serialized = stringArray[0];
                for (int i = 1; i < stringArray.Length; i++)
                {
                    serialized += "|" + stringArray[i];
                }
                return serialized;
            }
            return null;
        }

        public static T Deserialize<T>(byte[] buf) where T : new()
		{
			object obj = new T();
			int length = Marshal.SizeOf(obj);
			IntPtr ptr = Marshal.AllocHGlobal(length);
			Marshal.Copy(buf, 0, ptr, length);
			obj = Marshal.PtrToStructure(ptr, obj.GetType());
			Marshal.FreeHGlobal(ptr);
			return (T)obj;
		}

        // Deserializes string[]. If a class contains an array[] parameter in its
        // constructor, this must be called on the string it is serialized to.
        public static string[] Deserialize(string msg)
        {
            string[] deserializedString = msg.Split('|');
            return deserializedString;
        }

        // @func Malloc
        // @desc Allocate sizeof(obj) bytes. 
        public static byte[] Malloc<T>(T obj)
		{
			return new byte[Marshal.SizeOf(obj)];
		}
    }
}
