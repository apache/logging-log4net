#region Copyright & License
//
// Copyright 2001-2005 The Apache Software Foundation
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

using System.Data.SqlClient;
using log4net.Appender;
using log4net.Core;

namespace SampleAppendersApp.Appender
{
	/// <summary>
	/// Simple database appender
	/// </summary>
	/// <remarks>
	/// This database appender is very simple and does not support a configurable
	/// data schema. The schema supported is hardcoded into the appender.
	/// Also by not extending the AppenderSkeleton base class this appender
	/// avoids the serializable locking that it enforces.
	/// </remarks>
	public sealed class FastDbAppender : IAppender, IOptionHandler
	{
		private string m_name;
		private string m_connectionString;
		private SqlConnection m_dbConnection;

		public string Name
		{
			get { return m_name; }
			set { m_name = value; }
		}

		public string ConnectionString
		{
			get { return m_connectionString; }
			set { m_connectionString = value; }
		}

		public void ActivateOptions() 
		{
			m_dbConnection = new SqlConnection(m_connectionString);
			m_dbConnection.Open();
		}

		public void Close()
		{
			if (m_dbConnection != null)
			{
				m_dbConnection.Close();
			}
		}

		public void DoAppend(LoggingEvent loggingEvent)
		{
			SqlCommand command = m_dbConnection.CreateCommand();
			command.CommandText = "INSERT INTO [LogTable] ([Time],[Logger],[Level],[Thread],[Message]) VALUES (@Time,@Logger,@Level,@Thread,@Message)";

			command.Parameters.Add("@Time", loggingEvent.TimeStamp);
			command.Parameters.Add("@Logger", loggingEvent.LoggerName);
			command.Parameters.Add("@Level", loggingEvent.Level.Name);
			command.Parameters.Add("@Thread", loggingEvent.ThreadName);
			command.Parameters.Add("@Message", loggingEvent.RenderedMessage);

			command.ExecuteNonQuery();
		}
	}
}
