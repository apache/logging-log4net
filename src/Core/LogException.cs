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

using System;
using System.Runtime.Serialization;

namespace log4net.Core
{
	/// <summary>
	/// Exception base type for log4net.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This type extends <see cref="ApplicationException"/>. It
	/// does not add any new functionality but does differentiate the
	/// type of exception being thrown.</para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
#if !NETCF
	[Serializable]
#endif
	public class LogException : ApplicationException 
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LogException" /> class.
		/// </summary>
		public LogException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LogException" /> class with
		/// the specified message.
		/// </summary>
		/// <param name="message">A message to include with the exception.</param>
		public LogException(String message) : base(message) 
		{
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="LogException" /> class
		/// with the specified message and inner exception.
		/// </summary>
		/// <param name="message">A message to include with the exception.</param>
		/// <param name="innerException">A nested exception to include.</param>
		public LogException(String message, Exception innerException) : base(message, innerException) 
		{
		}

		#endregion Public Instance Constructors

		#region Protected Instance Constructors

#if !NETCF
		/// <summary>
		/// Initializes a new instance of the <see cref="LogException" /> class 
		/// with serialized data.
		/// </summary>
		/// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
		protected LogException(SerializationInfo info, StreamingContext context) : base(info, context) 
		{
		}
#endif

		#endregion Protected Instance Constructors
	}
}
