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

/*
 * Custom Logging Classes to support Event IDs.
 */

namespace log4net.Ext.EventID
{
	using System;

	public interface IEventIDLog : ILog
	{
		void Info(int eventId, object message);
		void Info(int eventId, object message, Exception t);

		void Warn(int eventId, object message);
		void Warn(int eventId, object message, Exception t);

		void Error(int eventId, object message);
		void Error(int eventId, object message, Exception t);

		void Fatal(int eventId, object message);
		void Fatal(int eventId, object message, Exception t);
	}
}

