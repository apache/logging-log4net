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

using log4net.Util;
using log4net.Core;

namespace log4net.Repository.Hierarchy
{
	/// <summary>
	/// The <see cref="RootLogger" /> sits at the top of the logger hierarchy. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <see cref="RootLogger" /> is a regular <see cref="Logger" /> except 
	/// that it provides several guarantees.
	/// </para>
	/// <para>
	/// First, it cannot be assigned a <c>null</c>
	/// level. Second, since the root logger cannot have a parent, the
	/// <see cref="EffectiveLevel"/> property always returns the value of the
	/// level field without walking the hierarchy.
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class RootLogger : Logger
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="RootLogger" /> class with
		/// the specified logging level.
		/// </summary>
		/// <param name="level">The level to assign to the root logger.</param>
		/// <remarks>
		/// The root logger names itself as "root". However, the root
		/// logger cannot be retrieved by name.
		/// </remarks>
		public RootLogger(Level level) : base("root")
		{
			this.Level = level;
		}

		#endregion Public Instance Constructors

		#region Override implementation of Logger

		/// <summary>
		/// Gets the assigned level value without walking the logger hierarchy.
		/// </summary>
		/// <value>The assigned level value without walking the logger hierarchy.</value>
		override public Level EffectiveLevel 
		{
			get 
			{
				return base.Level;
			}
		}

		/// <summary>
		/// Gets or sets the assigned <see cref="Level"/>, if any, for the root
		/// logger.  
		/// </summary>
		/// <value>
		/// The <see cref="Level"/> of the root logger.
		/// </value>
		/// <summary>
		/// Setting the level of the root logger to a null reference
		/// may have catastrophic results. We prevent this here.
		/// </summary>
		override public Level Level
		{
			get { return base.Level; }
			set
			{
				if (value == null) 
				{
					LogLog.Error("RootLogger: You have tried to set a null level to root.", new LogException());
				}
				else 
				{
					base.Level = value;
				}
			}
		}

		#endregion Override implementation of Logger
	}
}
