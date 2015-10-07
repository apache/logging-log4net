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
using System.Messaging;

using log4net.Core;

namespace SampleAppendersApp.Appender
{
	/// <summary>
	/// Appender writes to a Microsoft Message Queue
	/// </summary>
	/// <remarks>
	/// This appender sends log events via a specified MSMQ queue.
	/// The queue specified in the QueueName (e.g. .\Private$\log-test) must already exist on
	/// the source machine.
	/// The message label and body are rendered using separate layouts.
	/// </remarks>
	public class MsmqAppender : log4net.Appender.AppenderSkeleton
	{
		private MessageQueue m_queue;
		private string m_queueName;
		private log4net.Layout.PatternLayout m_labelLayout;

		public MsmqAppender()
		{
		}

		public string QueueName
		{
			get { return m_queueName; }
			set { m_queueName = value; }
		}

		public log4net.Layout.PatternLayout LabelLayout
		{
			get { return m_labelLayout; }
			set { m_labelLayout = value; }
		}

		override protected void Append(LoggingEvent loggingEvent) 
		{
			if (m_queue == null)
			{
				if (MessageQueue.Exists(m_queueName))
				{
					m_queue = new MessageQueue(m_queueName);
				}
				else
				{
					ErrorHandler.Error("Queue ["+m_queueName+"] not found");
				}
			}

			if (m_queue != null)
			{
				Message message = new Message();

				message.Label = RenderLabel(loggingEvent);

				using(System.IO.MemoryStream stream = new System.IO.MemoryStream())
				{
					System.IO.StreamWriter writer = new System.IO.StreamWriter(stream, new System.Text.UTF8Encoding(false, true));
					base.RenderLoggingEvent(writer, loggingEvent);
					writer.Flush();
					stream.Position = 0;
					message.BodyStream = stream;

					m_queue.Send(message);
				}
			}
		}

		private string RenderLabel(LoggingEvent loggingEvent)
		{
			if (m_labelLayout == null)
			{
				return null;
			}

			System.IO.StringWriter writer = new System.IO.StringWriter();
			m_labelLayout.Format(writer, loggingEvent);

			return writer.ToString();
		}
	}
}
