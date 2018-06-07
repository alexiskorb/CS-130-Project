using UnityEngine;

namespace Netcode {
	// @class SnapshotHistory
	// @desc Maintains the client's history of snapshots for client-side prediction and delta compression. 
	public class SnapshotHistory<T> where T : ISnapshot<T>, new() {
		// Time, in seconds, that the server should drop a client if it hasn't received a response from it.
		public static uint CLIENT_TIMEOUT = 5;

		// Size of the snapshot buffer.
		private uint m_predictionBufSize;
		// Snapshot buffer.
		private T[] m_snapshots;
		// Current sequence number.
		private uint m_seqno = 0;
		// Time elapsed since the last ack. 
		private float m_timeSinceLastAck = 0f;
		
		public SnapshotHistory(uint predictionBufSize)
		{
			m_predictionBufSize = predictionBufSize;
			m_snapshots = new T[predictionBufSize];
			PutSnapshot(new T());
		}

		public SnapshotHistory(uint predictionBufSize, T initialPlayerState)
		{
			m_predictionBufSize = predictionBufSize;
			m_snapshots = new T[predictionBufSize];
			PutSnapshot(initialPlayerState);
		}

		// @func Reconcile
		// @desc Reconciles the client state with the server state. If this returns false,
		// the client player needs to be rolled back to the server's snapshot.
		public bool Reconcile(T snapshot)
		{
			//T predicted = GetSnapshot(snapshot.m_seqno);
			//if (!predicted.Equals(snapshot)) {
			//	PutSnapshot(snapshot);
			//	return false;
			//} else
				return true;
		}

		// @func PutSnapshot
		// @desc Reset the ack timer, update the seqno, and record the snapshot. 
		public void PutSnapshot(T snapshot)
		{
			m_timeSinceLastAck = 0f;
			if (snapshot.m_seqno > m_seqno)
				m_seqno = snapshot.m_seqno;
			m_snapshots[snapshot.m_seqno % m_snapshots.Length] = snapshot;
		}

		// @func GetSnapshot
		// @desc Returns the snapshot with the given seqno. 
		// Unless the client is way ahead of the server - in which case
		// the client would most likely already be disconnected - the seqnos should
		// always match.
		private T GetSnapshot(uint seqno)
		{
			T snapshot = m_snapshots[seqno % m_predictionBufSize];
			if (snapshot.m_seqno != seqno)
				Debug.Log("<SnapshotHistory> Seqnos don't match.");
			return snapshot;
		}

		// @func GetMostRecentSnapshot
		// @desc Returns the snapshot that was inserted last.
		public T GetMostRecentSnapshot()
		{
			return m_snapshots[GetSnapshotIndex()];
		}

		// @func GetTimeSinceLastAck
		// @desc Returns the time since the last ack from the client associated with this snapshot history.
		public float GetTimeSinceLastAck()
		{
			return m_timeSinceLastAck;
		}

		// @func GetSeqno
		// @desc Get the current seqno.
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
			return GetSeqno() % m_predictionBufSize;
		}
	}
}
