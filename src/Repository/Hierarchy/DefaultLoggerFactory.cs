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

using log4net.Core;

namespace log4net.Repository.Hierarchy
{
	/// <summary>
	/// Implementation of DefaultLoggerFactory.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	internal class DefaultLoggerFactory : ILoggerFactory
	{
		#region Internal Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultLoggerFactory" /> class. 
		/// </summary>
		internal DefaultLoggerFactory()
		{
		}

		#endregion Internal Instance Constructors

		#region Implementation of ILoggerFactory

		/// <summary>
		/// Constructs a new <see cref="Logger" /> instance with the specified name.
		/// </summary>
		/// <param name="name">The name of the <see cref="Logger" />.</param>
		/// <returns>A new <see cref="Logger" /> instance.</returns>
		public Logger MakeNewLoggerInstance(string name) 
		{
			return new LoggerImpl(name);
		}

		#endregion

		internal class LoggerImpl : Logger
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="LoggerImpl" /> class
			/// with the specified name. 
			/// </summary>
			internal LoggerImpl(string name) : base(name)
			{
			}
		}
	}
}
