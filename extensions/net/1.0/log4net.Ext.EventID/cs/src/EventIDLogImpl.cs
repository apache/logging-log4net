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

using log4net.Core;

namespace log4net.Ext.EventID
{
	public class EventIDLogImpl : LogImpl, IEventIDLog
	{
		#region Public Instance Constructors

		public EventIDLogImpl(ILogger logger) : base(logger)
		{
		}

		#endregion Public Instance Constructors

		#region Implementation of IEventIDLog

		public void Info(int eventId, object message)
		{
			Info(eventId, message, null);
		}

		public void Info(int eventId, object message, System.Exception t)
		{
			if (this.IsInfoEnabled)
			{
				LoggingEvent loggingEvent = new LoggingEvent(FullName, Logger.Repository, Logger.Name, Level.Info, message, t);
				loggingEvent.EventProperties["EventID"] = eventId;
				Logger.Log(loggingEvent);
			}
		}

		public void Warn(int eventId, object message)
		{
			Warn(eventId, message, null);
		}

		public void Warn(int eventId, object message, System.Exception t)
		{
			if (this.IsWarnEnabled)
			{
				LoggingEvent loggingEvent = new LoggingEvent(FullName, Logger.Repository, Logger.Name, Level.Info, message, t);
				loggingEvent.EventProperties["EventID"] = eventId;
				Logger.Log(loggingEvent);
			}
		}

		public void Error(int eventId, object message)
		{
			Error(eventId, message, null);
		}

		public void Error(int eventId, object message, System.Exception t)
		{
			if (this.IsErrorEnabled)
			{
				LoggingEvent loggingEvent = new LoggingEvent(this.FullName, Logger.Repository, Logger.Name, Level.Info, message, t);
				loggingEvent.EventProperties["EventID"] = eventId;
				Logger.Log(loggingEvent);
			}
		}

		public void Fatal(int eventId, object message)
		{
			Fatal(eventId, message, null);
		}

		public void Fatal(int eventId, object message, System.Exception t)
		{
			if (this.IsFatalEnabled)
			{
				LoggingEvent loggingEvent = new LoggingEvent(this.FullName, Logger.Repository, Logger.Name, Level.Info, message, t);
				loggingEvent.EventProperties["EventID"] = eventId;
				Logger.Log(loggingEvent);
			}
		}

		#endregion Implementation of IEventIDLog
	}
}
