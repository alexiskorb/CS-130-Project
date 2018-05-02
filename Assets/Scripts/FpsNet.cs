using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace FpsNetcode {

	public static class Netcode {

		public enum PacketType {
			CONNECT,
			CLIENT_SNAPSHOT,
			CLIENT_CMD,
		}

		public enum CmdType {
			W, A, S, D,
			LEFT_CLICK
		}

		public struct PacketHeader {
			public PacketType type;
			public uint seqno;
		}

		public struct CmdPacket {
			public PacketHeader header;
			public CmdType cmd;
		}

		//[DllImport("msvcrt.dll", SetLastError = false)]
		//static extern void memcpy(void* dest, void* src, uint num);

		public static PacketHeader GetHeader(byte[] buf)
		{
			PacketHeader header = new PacketHeader();
			header.type = (PacketType)BitConverter.ToInt32(buf, 0);
			header.seqno = BitConverter.ToUInt32(buf, sizeof(PacketType));
			return header;
		}

		public static byte[] Serialize(CmdPacket cmdPacket)
		{
			byte[] buf = new byte[Marshal.SizeOf(cmdPacket)];
			MemCpy((int)cmdPacket.header.type, buf, 0, sizeof(PacketType));
			MemCpy(cmdPacket.header.seqno, buf, sizeof(PacketType), sizeof(uint));
			MemCpy((int)cmdPacket.cmd, buf, Marshal.SizeOf(cmdPacket.header), sizeof(int));
			return buf;
		}

		public static CmdPacket Deserialize(byte[] buf)
		{
			PacketHeader header = GetHeader(buf);
			CmdPacket cmdPacket = new CmdPacket();
			cmdPacket.header = header;
			cmdPacket.cmd = (CmdType)BitConverter.ToInt32(buf, Marshal.SizeOf(header));
			return cmdPacket;
		}

		public static void MemCpy(bool src, byte[] dst, int offset, int count)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, count);
		}

		public static void MemCpy(char src, byte[] dst, int offset, int count)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, count);
		}

		public static void MemCpy(double src, byte[] dst, int offset, int count)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, count);
		}

		public static void MemCpy(float src, byte[] dst, int offset, int count)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, count);
		}

		public static void MemCpy(int src, byte[] dst, int offset, int count)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, count);
		}

		public static void MemCpy(long src, byte[] dst, int offset, int count)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, count);
		}

		public static void MemCpy(short src, byte[] dst, int offset, int count)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, count);
		}
		public static void MemCpy(ushort src, byte[] dst, int offset, int count)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, count);
		}

		public static void MemCpy(ulong src, byte[] dst, int offset, int count)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, count);
		}

		public static void MemCpy(uint src, byte[] dst, int offset, int count)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(src), 0, dst, offset, count);
		}
	}

	// This is a class for executing a function every N seconds.
	// Particularly useful for network debugging...
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
