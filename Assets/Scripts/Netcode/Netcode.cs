using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
// @namespace Netcode
// @desc Netcode contains the packet data structures used by the client and server. 
// All packets must have a default constructor in order for serialization to work. 
namespace Netcode {
	public delegate void MainThreadWork();

	// @class MultiplayerGame
	// @desc Games should implement this interface in order to be alerted to network events. 
	public abstract class IMultiplayerGame : MonoBehaviour {
		public abstract void NetEvent(Snapshot snapshot);
		public abstract void NetEvent(ClientAddress clientAddr, PacketType packetType, byte[] buf);

		private Queue<byte[]> m_packetQueue = new Queue<byte[]>();

		// @func QueuePacket
		// @desc Queue a packet for the client to send. 
		public void QueuePacket<T>(T packet) where T : Packet
		{
			byte[] buf = Serializer.Serialize(packet);
			m_packetQueue.Enqueue(buf);
		}
	
		// @func RetrievePackets
		// @desc Clears the packet queue and returns the packets. Used by the client to
		// send game-related packets.
		public Queue<byte[]> GetPacketQueue()
		{
			Queue<byte[]> packetQueue = new Queue<byte[]>(m_packetQueue);
			m_packetQueue.Clear();
			return packetQueue;
		}
	}

	public enum PacketType : int {
		CREATE_LOBBY,
		REFRESH_LOBBY_LIST,
		JOIN_LOBBY,
		START_GAME,
		SNAPSHOT,
		COMMAND,
	}

	// @doc Consider naming commands after actions and not key presses
	// to leave open the possibility for client's having alternate key bindings. 
	public enum CmdType {
		FORWARD, BACKWARD,
		LEFT, RIGHT,
		PRIMARY_WEAPON,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class CreateLobby : Packet {
		public string m_lobbyName;
		public string m_hostPlayerName;
		public CreateLobby() { }
		public CreateLobby(string lobbyname, string hostPlayerName)
			: base(PacketType.CREATE_LOBBY, 0)
		{
			m_lobbyName = lobbyname;
			m_hostPlayerName = hostPlayerName;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class RefreshLobbyList : Packet {
		public string[] m_listOfGames;
		public RefreshLobbyList() { }
		public RefreshLobbyList(string[] listOfGames)
			: base(PacketType.REFRESH_LOBBY_LIST, 0)
		{
			m_listOfGames = listOfGames;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class JoinLobby : Packet {
		public string m_playerName;
		public string m_lobbyName;
		public JoinLobby() { }
		public JoinLobby(string playerName, string lobbyName)
			: base(PacketType.JOIN_LOBBY, 0)
		{
			m_playerName = playerName;
			m_lobbyName = lobbyName;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class StartGame : Packet {
		public int m_serverId;
		public StartGame() { }
		public StartGame(int serverId)
			: base(PacketType.START_GAME, 0)
		{
			m_serverId = serverId;
		}
	}

	// @doc A Snapshot is the state that is synchronized among clients and server.
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class Snapshot : ISnapshot<Snapshot> {
		public Vector3 m_position;
		public Vector3 m_eulerAngles;

		public Snapshot() { }
		// @doc Constructor must take this form or you'll get compiler errors. 
		// Static errors are better than runtime errors :)  
		public Snapshot(uint seqno, int serverId, GameObject gameObject)
			: base(serverId, PacketType.SNAPSHOT, seqno, gameObject) { }

		public override void Apply(ref GameObject gameObject)
		{
			gameObject.transform.position = m_position;
			gameObject.transform.eulerAngles = m_eulerAngles;
		}

		public override void FromPlayer(GameObject gameObject)
		{
			m_position = gameObject.transform.position;
			m_eulerAngles = gameObject.transform.eulerAngles;
		}

		public override bool Equals(Snapshot other)
		{
			return (m_position == other.m_position) &&
				(m_eulerAngles == other.m_eulerAngles);
		}
	}

	// @class SnapshotInterface
	// @desc Implement this interface to have snapshots integrated with the rest of the server.
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public abstract class ISnapshot<T> : Packet {
		public int m_serverId;
		public ISnapshot() { }
		public ISnapshot(int serverId, PacketType type, uint seqno, GameObject gameObject)
			: base(type, seqno)
		{
			m_serverId = serverId;
			FromPlayer(gameObject);
		}
		// @interface Equals
		// @desc Performs an equality test. 
		public abstract bool Equals(T other);
		// @interface FromPlayer
		// @desc Initialize the snapshot with a Game Object.
		public abstract void FromPlayer(GameObject gameObject);
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
		public uint m_seqno;

		public Packet() { }
		public Packet(PacketType type, uint seqno)
		{
			m_type = type;
			m_seqno = seqno;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class CmdPacket : Packet {
		public CmdType m_cmd;

		public CmdPacket() { }
		public CmdPacket(uint seqno, CmdType cmd)
			: base(PacketType.COMMAND, seqno)
		{
			m_cmd = cmd;
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

		// @func Malloc
		// @desc Allocate sizeof(obj) bytes. 
		public static byte[] Malloc<T>(T obj)
		{
			return new byte[Marshal.SizeOf(obj)];
		}
	}
}