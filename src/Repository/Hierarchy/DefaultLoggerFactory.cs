#region Copyright & License
//
// Copyright 2001-2004 The Apache Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
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
