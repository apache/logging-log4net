#region Copyright
//
// This framework is based on log4j see http://jakarta.apache.org/log4j
// Copyright (C) The Apache Software Foundation. All rights reserved.
//
// This software is published under the terms of the Apache Software
// License version 1.1, a copy of which has been included with this
// distribution in the LICENSE.txt file.
// 
#endregion

#if (!NETCF && !MONO)
#define HAS_READERWRITERLOCK
#endif

using System;

namespace log4net.Util
{
	/// <summary>
	/// Defines a lock that supports single writers and multiple readers
	/// </summary>
	/// <remarks>
	/// <para>
	/// <c>ReaderWriterLock</c> is used to synchronize access to a resource. 
	/// At any given time, it allows either concurrent read access for 
	/// multiple threads, or write access for a single thread. In a 
	/// situation where a resource is changed infrequently, a 
	/// <c>ReaderWriterLock</c> provides better throughput than a simple 
	/// one-at-a-time lock, such as <see cref="System.Threading.Monitor"/>.
	/// </para>
	/// <para>
	/// If a platform does not support a ReaderWriterLock implementation 
	/// then all readers and writers are serialized. Therefore the caller
	/// must not rely on multiple simultaneous readers.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	public sealed class ReaderWriterLock
	{
		#region Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ReaderWriterLock" /> class.
		/// </summary>
		public ReaderWriterLock()
		{
#if HAS_READERWRITERLOCK
			m_lock = new System.Threading.ReaderWriterLock();
#endif
		}

		#endregion Private Instance Constructors
  
		#region Public Methods

		/// <summary>
		/// Acquires a reader lock
		/// </summary>
		/// <remarks>
		/// AcquireReaderLock blocks if a different thread has the writer 
		/// lock, or if at least one thread is waiting for the writer lock
		/// </remarks>
		public void AcquireReaderLock()
		{
#if HAS_READERWRITERLOCK
			m_lock.AcquireReaderLock(-1);
#else
			System.Threading.Monitor.Enter(this);
#endif
		}

		/// <summary>
		/// Decrements the lock count
		/// </summary>
		/// <remarks>
		/// ReleaseReaderLock decrements the lock count. When the count 
		/// reaches zero, the lock is released
		/// </remarks>
		public void ReleaseReaderLock()
		{
#if HAS_READERWRITERLOCK
			m_lock.ReleaseReaderLock();
#else
			System.Threading.Monitor.Exit(this);
#endif
		}

		/// <summary>
		/// Acquires the writer lock
		/// </summary>
		/// <remarks>
		/// This method blocks if another thread has a reader lock or writer lock
		/// </remarks>
		public void AcquireWriterLock()
		{
#if HAS_READERWRITERLOCK
			m_lock.AcquireWriterLock(-1);
#else
			System.Threading.Monitor.Enter(this);
#endif
		}

		/// <summary>
		/// Decrements the lock count on the writer lock
		/// </summary>
		/// <remarks>
		/// ReleaseWriterLock decrements the writer lock count. 
		/// When the count reaches zero, the writer lock is released
		/// </remarks>
		public void ReleaseWriterLock()
		{
#if HAS_READERWRITERLOCK
			m_lock.ReleaseWriterLock();
#else
			System.Threading.Monitor.Exit(this);
#endif
		}

		#endregion Public Methods

		#region Private Members

#if HAS_READERWRITERLOCK
		private System.Threading.ReaderWriterLock m_lock;
#endif

		#endregion
	}
}
