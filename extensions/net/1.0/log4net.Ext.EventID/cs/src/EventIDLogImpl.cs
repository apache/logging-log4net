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
				loggingEvent.Properties["EventID"] = eventId;
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
				loggingEvent.Properties["EventID"] = eventId;
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
				loggingEvent.Properties["EventID"] = eventId;
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
				loggingEvent.Properties["EventID"] = eventId;
				Logger.Log(loggingEvent);
			}
		}

		#endregion Implementation of IEventIDLog
	}
}
