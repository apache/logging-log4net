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

namespace log4net.Ext.Trace
{
	public class TraceLogImpl : LogImpl, ITraceLog
	{
		#region Public Instance Constructors

		public TraceLogImpl(ILogger logger) : base(logger)
		{
		}

		#endregion Public Instance Constructors

		#region Implementation of ITraceLog

		public void Trace(object message)
		{
			Logger.Log(this.FullName, Level.Trace, message, null);
		}

		public void Trace(object message, System.Exception t)
		{
			Logger.Log(this.FullName, Level.Trace, message, t);
		}

		public bool IsTraceEnabled
		{
			get { return Logger.IsEnabledFor(Level.Trace); }
		}

		#endregion Implementation of ITraceLog
	}
}

