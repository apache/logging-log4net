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

