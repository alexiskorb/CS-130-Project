using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace FpsNetcode {

	// @class Netcode
	// @desc Netcode contains the packet data structures used by the client and server. 
	public static class Netcode {
		public enum PacketType : int {
			CONNECT,
			CLIENT_SNAPSHOT,
			CLIENT_CMD,
			DISCONNECT
		}

		// @doc Consider naming commands after actions and not key presses
		// to leave open the possibility for client's having alternate key bindings. 
		public enum CmdType {
			FORWARD, BACKWARD, LEFT, RIGHT,
			PRIMARY_WEAPON,
		}

		// @class SnapshotInterface
		// @desc Implement this interface to have snapshots 
		// integrated with the rest server. 
		public abstract class ISnapshot<T> : Packet {
			// The server ID of the snapshot. 
			public int m_serverId;

			// @interface Equals
			// @desc Performs an equality test. 
			public abstract bool Equals(T other);
			// @interface FromPlayer
			// @desc Initialize the snapshot with a Game Object.
			public abstract void FromPlayer(uint seqno, int serverId, GameObject gameObject);
			// @interface Apply
			// @desc Applies the snapshot to the Game Object.
			public abstract void Apply(ref GameObject gameObject);
		}

		// @doc A Snapshot is the state that is synchronized among clients and server.
		// Let's say you want to add new client state, such as weapon type and ammo. This is the only structure that has to be changed 
		// in the source code for that new state to be synchronized. Just implement the ISnapshot interface.
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public class Snapshot : ISnapshot<Snapshot> {
			public Vector3 m_position;
			public Vector3 m_eulerAngles;

			public Snapshot(byte[] buf)
			{
				Deserialize(buf);
			}

			public Snapshot(uint seqno, int serverId, GameObject gameObject)
			{
				FromPlayer(seqno, serverId, gameObject);
			}

			public override byte[] Serialize()
			{
				byte[] buf = Malloc(this);
				PacketHeader.Serialize(m_header, ref buf);
				MemCpy(m_serverId, buf, Marshal.SizeOf(m_header));
				MemCpy(m_position, buf, Marshal.SizeOf(m_header) + sizeof(int));
				MemCpy(m_eulerAngles, buf, Marshal.SizeOf(m_header) + sizeof(int) + Marshal.SizeOf(m_position));
				return buf;
			}

			public override void Deserialize(byte[] buf)
			{
				m_header = PacketHeader.Deserialize(buf);
				m_serverId = BitConverter.ToInt32(buf, Marshal.SizeOf(m_header));
				DeserializeVec3(ref m_position, buf, Marshal.SizeOf(m_header) + sizeof(int));
				DeserializeVec3(ref m_eulerAngles, buf, Marshal.SizeOf(m_header) + sizeof(int) + Marshal.SizeOf(m_position));
			}

			public override void Apply(ref GameObject gameObject)
			{
				gameObject.transform.position = m_position;
				gameObject.transform.eulerAngles = m_eulerAngles;
			}

			public override void FromPlayer(uint seqno, int serverId, GameObject gameObject)
			{
				m_header = new PacketHeader(PacketType.CLIENT_SNAPSHOT, seqno);
				m_serverId = serverId;
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

		// TODO: Try to make Packet a templated pattern and use the serializer class. 
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public abstract class Packet {
			public PacketHeader m_header;

			public abstract byte[] Serialize();
			public abstract void Deserialize(byte[] buf);
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public class CmdPacket : Packet {
			public CmdType m_cmd;

			public CmdPacket(byte[] buf)
			{
				Deserialize(buf);
			}

			public CmdPacket(uint seqno, CmdType cmd)
			{
				m_header = new PacketHeader(PacketType.CLIENT_CMD, seqno);
				m_cmd = cmd;
			}

			public override byte[] Serialize()
			{
				byte[] buf = Malloc(this);
				PacketHeader.Serialize(m_header, ref buf);
				MemCpy((int)m_cmd, buf, Marshal.SizeOf(m_header));
				return buf;
			}

			public override void Deserialize(byte[] buf)
			{
				m_header = PacketHeader.Deserialize(buf);
				m_cmd = (CmdType)BitConverter.ToInt32(buf, Marshal.SizeOf(m_header));
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public class Disconnect : Packet {
			public int m_serverId;

			public Disconnect(byte[] buf)
			{
				Deserialize(buf);
			}

			public Disconnect(uint seqno, int serverId)
			{
				m_header = new PacketHeader(PacketType.DISCONNECT, seqno);
				m_serverId = serverId;
			}

			public override byte[] Serialize()
			{
				byte[] buf = Malloc(this);
				PacketHeader.Serialize(m_header, ref buf);
				MemCpy(m_serverId, buf, Marshal.SizeOf(m_header));
				return buf;
			}

			public override void Deserialize(byte[] buf)
			{
				m_header = PacketHeader.Deserialize(buf);
				m_serverId = BitConverter.ToInt32(buf, Marshal.SizeOf(m_header));
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public class Connect : Packet {
			public int m_serverId;

			public Connect(byte[] buf)
			{
				Deserialize(buf);
			}

			public Connect(uint seqno, int serverId)
			{
				m_header = new PacketHeader(PacketType.CONNECT, seqno);
				m_serverId = serverId;
			}

			public override byte[] Serialize()
			{
				byte[] buf = Malloc(this);
				PacketHeader.Serialize(m_header, ref buf);
				MemCpy(m_serverId, buf, Marshal.SizeOf(m_header));
				return buf;
			}

			public override void Deserialize(byte[] buf)
			{
				m_header = PacketHeader.Deserialize(buf);
				m_serverId = BitConverter.ToInt32(buf, Marshal.SizeOf(m_header));
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public class PacketHeader {
			public PacketType m_type;
			public uint m_seqno;

			public PacketHeader(PacketType type, uint seqno)
			{
				m_type = type;
				m_seqno = seqno;
			}

			// @func Serialize
			// @desc Serializes a PacketHeader. Assumes dst is already backed by memory. 
			public static void Serialize(PacketHeader header, ref byte[] dst)
			{
				MemCpy((int)header.m_type, dst, 0);
				MemCpy(header.m_seqno, dst, sizeof(PacketType));
			}

			public static byte[] Serialize(PacketHeader header)
			{
				byte[] buf = Malloc(header);
				MemCpy((int)header.m_type, buf, 0);
				MemCpy(header.m_seqno, buf, sizeof(PacketType));
				return buf;
			}

			public static PacketHeader Deserialize(byte[] buf)
			{
				PacketType type = (PacketType)BitConverter.ToInt32(buf, 0);
				uint seqno = BitConverter.ToUInt32(buf, sizeof(PacketType));
				return new PacketHeader(type, seqno);
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
				m_seqno = initialPlayerState.m_header.m_seqno;
				PutSnapshot(initialPlayerState);
			}

			// @func Reconcile
			// @desc Reconciles the client state with the server state. If this returns false,
			// the client player needs to be rolled back to the server's snapshot.
			public bool Reconcile(T snapshot)
			{
				T predicted = GetSnapshot(snapshot.m_header.m_seqno);
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
				if (snapshot.m_header.m_seqno > m_seqno)
					m_seqno = snapshot.m_header.m_seqno;
				m_snapshots[snapshot.m_header.m_seqno % MAX_SNAPSHOTS] = snapshot;
			}

			// @func GetSnapshot
			// @desc Returns the snapshot with the given seqno. 
			// Unless the client is way ahead of the server - in which case
			// the client would most likely already be disconnected - the seqnos should
			// always match. 
			private T GetSnapshot(uint seqno)
			{
				T snapshot = m_snapshots[seqno % MAX_SNAPSHOTS];
				if (snapshot.m_header.m_seqno != seqno)
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
