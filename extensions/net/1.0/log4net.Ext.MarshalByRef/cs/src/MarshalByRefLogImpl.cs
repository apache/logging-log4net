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
using log4net.Repository;
using log4net.Util;

namespace log4net.Ext.MarshalByRef
{
	/// <summary>
	/// Marshal By Reference implementation of <see cref="ILog"/>
	/// </summary>
	/// <remarks>
	/// <para>
	/// Logger wrapper that is <see cref="MarshalByRefObject"/>. These objects
	/// can be passed by reference across a remoting boundary.
	/// </para>
	/// </remarks>
	public sealed class MarshalByRefLogImpl : MarshalByRefObject, ILog
	{
		private readonly static Type ThisDeclaringType = typeof(MarshalByRefLogImpl);
		private readonly ILogger m_logger;
		private Level m_levelDebug;
		private Level m_levelInfo;
		private Level m_levelWarn;
		private Level m_levelError;
		private Level m_levelFatal;

		#region Public Instance Constructors

		public MarshalByRefLogImpl(ILogger logger)
		{
			m_logger = logger;

			// Listen for changes to the repository
			logger.Repository.ConfigurationChanged += new LoggerRepositoryConfigurationChangedEventHandler(LoggerRepositoryConfigurationChanged);

			// load the current levels
			ReloadLevels(logger.Repository);
		}

		#endregion Public Instance Constructors

		private void ReloadLevels(ILoggerRepository repository)
		{
			LevelMap levelMap = repository.LevelMap;

			m_levelDebug = levelMap.LookupWithDefault(Level.Debug);
			m_levelInfo = levelMap.LookupWithDefault(Level.Info);
			m_levelWarn = levelMap.LookupWithDefault(Level.Warn);
			m_levelError = levelMap.LookupWithDefault(Level.Error);
			m_levelFatal = levelMap.LookupWithDefault(Level.Fatal);
		}

		private void LoggerRepositoryConfigurationChanged(object sender, EventArgs e)
		{
			ILoggerRepository repository = sender as ILoggerRepository;
			if (repository != null)
			{
				ReloadLevels(repository);
			}
		}

		#region Implementation of ILog

		public void Debug(object message) 
		{
			Logger.Log(ThisDeclaringType, m_levelDebug, message, null);
		}

		public void Debug(object message, Exception t) 
		{
			Logger.Log(ThisDeclaringType, m_levelDebug, message, t);
		}

		public void DebugFormat(string format, params object[] args) 
		{
			if (IsDebugEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelDebug, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
			}
		}

		public void DebugFormat(string format, object arg0) 
		{
			if (IsDebugEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelDebug, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0 }), null);
			}
		}

		public void DebugFormat(string format, object arg0, object arg1) 
		{
			if (IsDebugEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelDebug, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1 }), null);
			}
		}

		public void DebugFormat(string format, object arg0, object arg1, object arg2) 
		{
			if (IsDebugEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelDebug, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1, arg2 }), null);
			}
		}

		public void DebugFormat(IFormatProvider provider, string format, params object[] args) 
		{
			if (IsDebugEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelDebug, new SystemStringFormat(provider, format, args), null);
			}
		}

		public void Info(object message) 
		{
			Logger.Log(ThisDeclaringType, m_levelInfo, message, null);
		}
  
		public void Info(object message, Exception t) 
		{
			Logger.Log(ThisDeclaringType, m_levelInfo, message, t);
		}

		public void InfoFormat(string format, params object[] args) 
		{
			if (IsInfoEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelInfo, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
			}
		}

		public void InfoFormat(string format, object arg0) 
		{
			if (IsInfoEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelInfo, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0 }), null);
			}
		}

		public void InfoFormat(string format, object arg0, object arg1) 
		{
			if (IsInfoEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelInfo, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1 }), null);
			}
		}

		public void InfoFormat(string format, object arg0, object arg1, object arg2) 
		{
			if (IsInfoEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelInfo, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1, arg2 }), null);
			}
		}

		public void InfoFormat(IFormatProvider provider, string format, params object[] args) 
		{
			if (IsInfoEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelInfo, new SystemStringFormat(provider, format, args), null);
			}
		}

		public void Warn(object message) 
		{
			Logger.Log(ThisDeclaringType, m_levelWarn, message, null);
		}
  
		public void Warn(object message, Exception t) 
		{
			Logger.Log(ThisDeclaringType, m_levelWarn, message, t);
		}

		public void WarnFormat(string format, params object[] args) 
		{
			if (IsWarnEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelWarn, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
			}
		}

		public void WarnFormat(string format, object arg0) 
		{
			if (IsWarnEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelWarn, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0 }), null);
			}
		}

		public void WarnFormat(string format, object arg0, object arg1) 
		{
			if (IsWarnEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelWarn, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1 }), null);
			}
		}

		public void WarnFormat(string format, object arg0, object arg1, object arg2) 
		{
			if (IsWarnEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelWarn, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1, arg2 }), null);
			}
		}

		public void WarnFormat(IFormatProvider provider, string format, params object[] args) 
		{
			if (IsWarnEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelWarn, new SystemStringFormat(provider, format, args), null);
			}
		}

		public void Error(object message) 
		{
			Logger.Log(ThisDeclaringType, m_levelError, message, null);
		}

		public void Error(object message, Exception t) 
		{
			Logger.Log(ThisDeclaringType, m_levelError, message, t);
		}

		public void ErrorFormat(string format, params object[] args) 
		{
			if (IsErrorEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelError, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
			}
		}

		public void ErrorFormat(string format, object arg0) 
		{
			if (IsErrorEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelError, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0 }), null);
			}
		}

		public void ErrorFormat(string format, object arg0, object arg1) 
		{
			if (IsErrorEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelError, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1 }), null);
			}
		}

		public void ErrorFormat(string format, object arg0, object arg1, object arg2) 
		{
			if (IsErrorEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelError, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1, arg2 }), null);
			}
		}

		public void ErrorFormat(IFormatProvider provider, string format, params object[] args) 
		{
			if (IsErrorEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelError, new SystemStringFormat(provider, format, args), null);
			}
		}

		public void Fatal(object message) 
		{
			Logger.Log(ThisDeclaringType, m_levelFatal, message, null);
		}
  
		public void Fatal(object message, Exception t) 
		{
			Logger.Log(ThisDeclaringType, m_levelFatal, message, t);
		}

		public void FatalFormat(string format, params object[] args) 
		{
			if (IsFatalEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelFatal, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
			}
		}

		public void FatalFormat(string format, object arg0) 
		{
			if (IsFatalEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelFatal, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0 }), null);
			}
		}

		public void FatalFormat(string format, object arg0, object arg1) 
		{
			if (IsFatalEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelFatal, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1 }), null);
			}
		}

		public void FatalFormat(string format, object arg0, object arg1, object arg2) 
		{
			if (IsFatalEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelFatal, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1, arg2 }), null);
			}
		}

		public void FatalFormat(IFormatProvider provider, string format, params object[] args) 
		{
			if (IsFatalEnabled)
			{
				Logger.Log(ThisDeclaringType, m_levelFatal, new SystemStringFormat(provider, format, args), null);
			}
		}

		public bool IsDebugEnabled
		{
			get { return Logger.IsEnabledFor(m_levelDebug); }
		}
  
		public bool IsInfoEnabled
		{
			get { return Logger.IsEnabledFor(m_levelInfo); }
		}

		public bool IsWarnEnabled
		{
			get { return Logger.IsEnabledFor(m_levelWarn); }
		}

		public bool IsErrorEnabled
		{
			get { return Logger.IsEnabledFor(m_levelError); }
		}

		public bool IsFatalEnabled
		{
			get { return Logger.IsEnabledFor(m_levelFatal); }
		}

		#endregion Implementation of ILog

		#region Implementation of ILoggerWrapper

		public ILogger Logger
		{
			get { return m_logger; }
		}

		#endregion

		/// <summary>
		/// Live forever
		/// </summary>
		/// <returns><c>null</c></returns>
		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
