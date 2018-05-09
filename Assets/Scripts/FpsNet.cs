using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace FpsNetcode {
	public static class Netcode {
		// @TODO This part of Netcode is heavily game-specific, so we'll probably want to move
		// this code to the Game class. 

		// @func ApplySnapshot
		// @desc Applies the given snapshot to the game object. 
		public static void ApplySnapshot(ref GameObject gameObject, PlayerSnapshot snapshot)
		{
			gameObject.transform.position = snapshot.m_position;
		}

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

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public class CmdPacket {
			public PacketHeader m_header;	
			public CmdType m_cmd;

			public CmdPacket(uint seqno, CmdType cmd)
			{
				m_header = new PacketHeader(PacketType.CLIENT_CMD, seqno);
				m_cmd = cmd;
			}

			private CmdPacket(PacketHeader header, CmdType cmd)
			{
				m_header = header;
				m_cmd = cmd;
			}

			public static byte[] Serialize(CmdPacket cmdPacket)
			{
				byte[] buf = Malloc(cmdPacket);
				PacketHeader.Serialize(cmdPacket.m_header, ref buf);
				MemCpy((int)cmdPacket.m_cmd, buf, Marshal.SizeOf(cmdPacket.m_header));
				return buf;
			}

			public static CmdPacket Deserialize(byte[] buf)
			{
				PacketHeader header = PacketHeader.Deserialize(buf);
				CmdType cmd = (CmdType)BitConverter.ToInt32(buf, Marshal.SizeOf(header));
				return new CmdPacket(header, cmd);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public class Disconnect {
			public PacketHeader m_header;
			public int m_serverId;

			public Disconnect(uint seqno, int serverId)
			{
				m_header = new PacketHeader(PacketType.DISCONNECT, seqno);
				m_serverId = serverId;
			}

			private Disconnect(PacketHeader header, int serverId)
			{
				m_header = header;
				m_serverId = serverId;
			}

			public static byte[] Serialize(Disconnect disconnect)
			{
				byte[] buf = Malloc(disconnect);
				PacketHeader.Serialize(disconnect.m_header, ref buf);
				MemCpy(disconnect.m_serverId, buf, Marshal.SizeOf(disconnect.m_header));
				return buf;
			}

			public static Disconnect Deserialize(byte[] buf)
			{
				PacketHeader header = PacketHeader.Deserialize(buf);
				int serverId = BitConverter.ToInt32(buf, Marshal.SizeOf(header));
				return new Disconnect(header, serverId);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public class PlayerSnapshot {
			public PacketHeader m_header;
			public int m_serverId;
			public Vector3 m_position;

			public PlayerSnapshot(uint seqno, int serverId, Vector3 position)
			{
				m_header = new PacketHeader(PacketType.CLIENT_SNAPSHOT, seqno);
				m_serverId = serverId;
				m_position = position;
			}

			private PlayerSnapshot(PacketHeader header, int serverId, Vector3 position)
			{
				m_header = header;
				m_serverId = serverId;
				m_position = position;
			}

			public static byte[] Serialize(PlayerSnapshot snapshot)
			{
				byte[] buf = Malloc(snapshot);
				PacketHeader.Serialize(snapshot.m_header, ref buf);
				MemCpy(snapshot.m_serverId, buf, Marshal.SizeOf(snapshot.m_header));
				MemCpy(snapshot.m_position, buf, Marshal.SizeOf(snapshot.m_header) + sizeof(int));
				return buf;
			}

			public static PlayerSnapshot Deserialize(byte[] buf)
			{
				PacketHeader header = PacketHeader.Deserialize(buf);
				Vector3 position = new Vector3();
				int serverId = BitConverter.ToInt32(buf, Marshal.SizeOf(header));
				DeserializeVec3(ref position, buf, Marshal.SizeOf(header) + sizeof(int));
				return new PlayerSnapshot(header, serverId, position);
			}
		}

		// @doc Everything after this is game-independent and shouldn't really be touched. 

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public class Connect {
			public PacketHeader m_header;
			public int m_serverId;

			public Connect(uint seqno, int serverId)
			{
				m_header = new PacketHeader(PacketType.CONNECT, seqno);
				m_serverId = serverId;
			}

			private Connect(PacketHeader header, int serverId)
			{
				m_header = header;
				m_serverId = serverId;
			}

			public static byte[] Serialize(Connect connect)
			{
				byte[] buf = Malloc(connect);
				PacketHeader.Serialize(connect.m_header, ref buf);
				MemCpy(connect.m_serverId, buf, Marshal.SizeOf(connect.m_header));
				return buf;
			}

			public static Connect Deserialize(byte[] buf)
			{
				PacketHeader header = PacketHeader.Deserialize(buf);
				int serverId = BitConverter.ToInt32(buf, Marshal.SizeOf(header));
				return new Connect(header, serverId);
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
		public class ClientHistory {
			public static uint CLIENT_TIMEOUT = 5;
			private static uint MAX_SNAPSHOTS = 10;

			private PlayerSnapshot[] m_snapshots = new PlayerSnapshot[MAX_SNAPSHOTS];
			private uint m_seqno = 0;
			private float m_timeSinceLastAck = 0f;

			public ClientHistory(PlayerSnapshot initialPlayerState)
			{
				m_seqno = initialPlayerState.m_header.m_seqno;
				PutSnapshot(initialPlayerState);
			}

			// @func Reconcile
			// @desc Reconciles the client state with the server state. If this returns false,
			// the client player needs to be rolled back to the server's snapshot.
			public bool Reconcile(PlayerSnapshot snapshot)
			{
				PlayerSnapshot predicted = GetSnapshot(snapshot.m_header.m_seqno);
				if (predicted.m_position != snapshot.m_position) {
					PutSnapshot(snapshot);
					return false;
				} else
					return true;
			}

			public void PutSnapshot(PlayerSnapshot playerSnapshot)
			{
				m_timeSinceLastAck = 0f;
				if (playerSnapshot.m_header.m_seqno > m_seqno)
					m_seqno = playerSnapshot.m_header.m_seqno;
				m_snapshots[playerSnapshot.m_header.m_seqno % MAX_SNAPSHOTS] = playerSnapshot;
			}

			public PlayerSnapshot GetSnapshot(uint seqno)
			{
				PlayerSnapshot snapshot = m_snapshots[seqno % MAX_SNAPSHOTS];
				if (snapshot.m_header.m_seqno != seqno)
					Debug.Log("ClientHistory: Seqnos don't match.");
				return snapshot;
			}

			public PlayerSnapshot GetMostRecentSnapshot()
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
			public string ipAddress;
			public int port;

			public ClientAddress(string ipAddress, int port)
			{
				this.ipAddress = ipAddress;
				this.port = port;
			}
		}

		// @doc Helpers for copying primitive types to byte arrays. 

		public static void DeserializeVec3(ref Vector3 src, byte[] dst, int offset)
		{
			src.x = BitConverter.ToSingle(dst, offset + 0 * sizeof(float));
			src.y = BitConverter.ToSingle(dst, offset + 1 * sizeof(float));
			src.z = BitConverter.ToSingle(dst, offset + 2 * sizeof(float));
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
	}
}
	