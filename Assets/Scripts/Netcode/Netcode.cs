using UnityEngine;
using System;
using System.Runtime.InteropServices;

// @namespace Netcode
// @desc Netcode contains the packet data structures used by the client and server. 
// All packets must have a default constructor in order for serialization to work. 
namespace Netcode {
	public delegate void MainThreadWork();

	// @class MultiplayerGame
	// @desc Games should implement this interface in order to be alerted to network events. 
	public abstract class IMultiplayerGame : MonoBehaviour {
		public abstract GameObject NetEvent(Connect connect);
		public abstract GameObject NetEvent(Disconnect disconnect);
		public abstract GameObject NetEvent(Snapshot snapshot);
	}

	public enum PacketType : int {
		CONNECT,
		SNAPSHOT,
		COMMAND,
		DISCONNECT
	}

	// @doc Consider naming commands after actions and not key presses
	// to leave open the possibility for client's having alternate key bindings. 
	public enum CmdType {
		FORWARD, BACKWARD, LEFT, RIGHT,
		PRIMARY_WEAPON,
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

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class Disconnect : Packet {
		public int m_serverId;

		public Disconnect() { }
		public Disconnect(uint seqno, int serverId)
			: base(PacketType.DISCONNECT, seqno)
		{
			m_serverId = serverId;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class Connect : Packet {
		public int m_serverId;

		public Connect() { }
		public Connect(uint seqno, int serverId)
			: base(PacketType.CONNECT, seqno)
		{
			m_serverId = serverId;
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