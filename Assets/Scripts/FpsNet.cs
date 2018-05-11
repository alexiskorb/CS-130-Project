using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace FpsNetcode {

	// @class Netcode
	// @desc Netcode contains the packet data structures used by the client and server. 
	// All packets must have a default constructor in order for serialization to work. 
	public static class Netcode {
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

		// @class SnapshotInterface
		// @desc Implement this interface to have snapshots integrated with the rest server.
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public abstract class ISnapshot<T> : Packet {
			// The server ID of the snapshot. 
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

		// @doc A Snapshot is the state that is synchronized among clients and server.
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public class Snapshot : ISnapshot<Snapshot> {
			public Vector3 m_position;
			public Vector3 m_eulerAngles;

			public Snapshot() {}
			// @doc Constructor must take this form or you'll get compiler errors. 
			public Snapshot(uint seqno, int serverId, GameObject gameObject)
				: base(serverId, PacketType.SNAPSHOT, seqno, gameObject) {}

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

		// ==== @doc Everything after this is game-independent and shouldn't really be touched. ====
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

		// @class ClientHistory
		// @desc Maintains the client's history of snapshots for client-side prediction and delta compression. 
		public class ClientHistory<T> where T : ISnapshot<T> {
			public static uint CLIENT_TIMEOUT = 5;
			private static uint MAX_SNAPSHOTS = 10;

			private T[] m_snapshots = new T[MAX_SNAPSHOTS];
			private uint m_seqno = 0;
			private float m_timeSinceLastAck = 0f;

			public ClientHistory(T initialPlayerState)
			{
				m_seqno = initialPlayerState.m_seqno;
				PutSnapshot(initialPlayerState);
			}

			// @func Reconcile
			// @desc Reconciles the client state with the server state. If this returns false,
			// the client player needs to be rolled back to the server's snapshot.
			public bool Reconcile(T snapshot)
			{
				T predicted = GetSnapshot(snapshot.m_seqno);
				if (!predicted.Equals(snapshot)) {
					PutSnapshot(snapshot);
					return false;
				} else
					return true;
			}

			// @func PutSnapshot
			// @desc Reset the ack timer, update the seqno, and record the snapshot. 
			public void PutSnapshot(T snapshot)
			{
				m_timeSinceLastAck = 0f;
				if (snapshot.m_seqno > m_seqno)
					m_seqno = snapshot.m_seqno;
				m_snapshots[snapshot.m_seqno % MAX_SNAPSHOTS] = snapshot;
			}

			// @func GetSnapshot
			// @desc Returns the snapshot with the given seqno. 
			// Unless the client is way ahead of the server - in which case
			// the client would most likely already be disconnected - the seqnos should
			// always match. 
			private T GetSnapshot(uint seqno)
			{
				T snapshot = m_snapshots[seqno % MAX_SNAPSHOTS];
				if (snapshot.m_seqno != seqno)
					Debug.Log("<ClientHistory> Seqnos don't match.");
				return snapshot;
			}

			public T GetMostRecentSnapshot()
			{
				return m_snapshots[GetSnapshotIndex()];
			}

			public float GetTimeSinceLastAck()
			{
				return m_timeSinceLastAck;
			}

			public uint GetSeqno()
			{
				return m_seqno;
			}

			// @func IncrTimeSinceLastAck
			// @desc Call this every frame in the server's update function. 
			public void IncrTimeSinceLastAck(float dt)
			{
				m_timeSinceLastAck += dt;
			}

			// @func GetSnapshotIndex
			// @desc Gets the index into the circular buffer of the most recent snapshot.
			private uint GetSnapshotIndex()
			{
				return GetSeqno() % MAX_SNAPSHOTS;
			}
		}

		public delegate void MainThreadWork();

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

		// @doc Helpers for copying primitive types to byte arrays. 

		public static void DeserializeVec3(ref Vector3 src, byte[] dst, int offset)
		{
			src = new Vector3 {
				x = BitConverter.ToSingle(dst, offset + 0 * sizeof(float)),
				y = BitConverter.ToSingle(dst, offset + 1 * sizeof(float)),
				z = BitConverter.ToSingle(dst, offset + 2 * sizeof(float))
			};
		}

		// @func Malloc
		// @desc Allocate sizeof(obj) bytes. 
		public static byte[] Malloc<T>(T obj)
		{
			return new byte[Marshal.SizeOf(obj)];
		}

		public static void MemCpy(Vector3 src, byte[] dst, int offset)
		{
			MemCpy(src.x, dst, offset + 0 * sizeof(float));
			MemCpy(src.y, dst, offset + 1 * sizeof(float));
			MemCpy(src.z, dst, offset + 2 * sizeof(float));
		}

		public static void MemCpy(bool src, byte[] dst, int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, sizeof(bool));
		}

		public static void MemCpy(char src, byte[] dst, int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, sizeof(char));
		}

		public static void MemCpy(double src, byte[] dst, int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, sizeof(double));
		}

		public static void MemCpy(float src, byte[] dst, int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, sizeof(float));
		}

		public static void MemCpy(int src, byte[] dst, int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, sizeof(int));
		}

		public static void MemCpy(long src, byte[] dst, int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, sizeof(long));
		}

		public static void MemCpy(short src, byte[] dst, int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, sizeof(short));
		}
		public static void MemCpy(ushort src, byte[] dst, int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, sizeof(ushort));
		}

		public static void MemCpy(ulong src, byte[] dst, int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, sizeof(ulong));
		}

		public static void MemCpy(uint src, byte[] dst, int offset)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, sizeof(uint));
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
}

// @source http://thecodeisart.blogspot.com/2008/11/with-this-class-you-can-easy-convert.html
// (With some modifications)
public static class Serializer {
	public static byte[] Serialize<T>(T obj)
	{
		byte[] buf = FpsNetcode.Netcode.Malloc(obj);
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
}