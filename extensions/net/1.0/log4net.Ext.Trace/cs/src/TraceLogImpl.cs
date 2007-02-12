#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
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
using System.Globalization;

using log4net.Core;
using log4net.Util;

namespace log4net.Ext.Trace
{
	public class TraceLogImpl : LogImpl, ITraceLog
	{
		/// <summary>
		/// The fully qualified name of this declaring type not the type of any subclass.
		/// </summary>
		private readonly static Type ThisDeclaringType = typeof(TraceLogImpl);

		/// <summary>
		/// The default value for the TRACE level
		/// </summary>
		private readonly static Level s_defaultLevelTrace = new Level(20000, "TRACE");
		
		/// <summary>
		/// The current value for the TRACE level
		/// </summary>
		private Level m_levelTrace;
		

		public TraceLogImpl(ILogger logger) : base(logger)
		{
		}

		/// <summary>
		/// Lookup the current value of the TRACE level
		/// </summary>
		protected override void ReloadLevels(log4net.Repository.ILoggerRepository repository)
		{
			base.ReloadLevels(repository);

			m_levelTrace = repository.LevelMap.LookupWithDefault(s_defaultLevelTrace);
		}

		#region Implementation of ITraceLog

		public void Trace(object message)
		{
			Logger.Log(ThisDeclaringType, m_levelTrace, message, null);
		}

		public void Trace(object message, System.Exception t)
		{
			Logger.Log(ThisDeclaringType, m_levelTrace, message, t);
		}

		public void TraceFormat(string format, params object[] args)
		{
			if (IsTraceEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelTrace, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
			}
		}

		public void TraceFormat(IFormatProvider provider, string format, params object[] args)
		{
			if (IsTraceEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelTrace, new SystemStringFormat(provider, format, args), null);
			}
		}

		public bool IsTraceEnabled
		{
			get { return Logger.IsEnabledFor(m_levelTrace); }
		}

		#endregion Implementation of ITraceLog
	}
}

