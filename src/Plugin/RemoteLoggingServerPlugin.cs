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

// .NET Compact Framework 1.0 has no support for System.Runtime.Remoting
#if !NETCF

using System;
using System.Runtime.Remoting;

using log4net.Util;
using log4net.Repository;
using log4net.Core;
using IRemoteLoggingSink = log4net.Appender.RemotingAppender.IRemoteLoggingSink;

namespace log4net.Plugin
{
	/// <summary>
	/// Publishes an instance of <see cref="IRemoteLoggingSink"/> 
	/// on the specified URI.
	/// </summary>
	/// <author>Nicko Cadell</author>
	/// <author>Gert Driesen</author>
	public class RemoteLoggingServerPlugin : PluginSkeleton
	{
		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteLoggingServerPlugin" /> class.
		/// </summary>
		public RemoteLoggingServerPlugin() : base("RemoteLoggingServerPlugin:Unset URI")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteLoggingServerPlugin" /> class
		/// with specified name.
		/// </summary>
		/// <param name="sinkUri">The name to publish the sink under in the remoting infrastructure. 
		/// See <see cref="SinkUri"/> for more details.</param>
		public RemoteLoggingServerPlugin(string sinkUri) : base("RemoteLoggingServerPlugin:"+sinkUri)
		{
			m_sinkUri = sinkUri;
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets or sets the URI of this sink.
		/// </summary>
		/// <value>
		/// The URI of this sink.
		/// </value>
		/// <remarks>
		/// <para>
		/// This is the name under which the object is marshaled.
		/// <see cref="RemotingServices.Marshal(MarshalByRefObject,String,Type)"/>
		/// </para>
		/// </remarks>
		public virtual string SinkUri 
		{ 
			get { return m_sinkUri; }
			set { m_sinkUri = value; }
		}

		#endregion Public Instance Properties

		#region Override implementation of PluginSkeleton

		/// <summary>
		/// Attaches this plugin to a <see cref="ILoggerRepository"/>.
		/// </summary>
		/// <param name="repository">The <see cref="ILoggerRepository"/> that this plugin should be attached to.</param>
		/// <remarks>
		/// <para>
		/// A plugin may only be attached to a single repository.
		/// </para>
		/// <para>
		/// This method is called when the plugin is attached to the repository.
		/// </para>
		/// </remarks>
		override public void Attach(ILoggerRepository repository)
		{
			base.Attach(repository);

			// Create the sink and marshal it
			m_sink = new RemoteLoggingSinkImpl(repository); 

			try
			{
				RemotingServices.Marshal(m_sink, m_sinkUri, typeof(IRemoteLoggingSink));		
			}
			catch(Exception ex)
			{
				LogLog.Error("RemoteLoggingServerPlugin: Failed to Marshal remoting sink", ex);
			}
		}

		/// <summary>
		/// Is called when the plugin is to shutdown.
		/// </summary>
		override public void Shutdown()
		{
			// Stops the sink from receiving messages
			RemotingServices.Disconnect(m_sink);
			m_sink = null;

			base.Shutdown();
		}

		#endregion Override implementation of PluginSkeleton

		#region Private Instance Fields

		private RemoteLoggingSinkImpl m_sink;
		private string m_sinkUri;

		#endregion Private Instance Fields

		/// <summary>
		/// Delivers <see cref="LoggingEvent"/> objects to a remote sink.
		/// </summary>
		private class RemoteLoggingSinkImpl : MarshalByRefObject, IRemoteLoggingSink
		{
			#region Public Instance Constructors

			/// <summary>
			/// Initializes a new instance of the <see cref="RemoteLoggingSinkImpl"/> for the
			/// specified <see cref="ILoggerRepository"/>.
			/// </summary>
			/// <param name="repository">The repository to log to.</param>
			public RemoteLoggingSinkImpl(ILoggerRepository repository)
			{
				m_repository = repository;
			}

			#endregion Public Instance Constructors

			#region Implementation of IRemoteLoggingSink

			/// <summary>
			/// Logs the events to the repository.
			/// </summary>
			/// <param name="events">The events to log.</param>
			/// <remarks>
			/// The events passed are logged to the <see cref="ILoggerRepository"/>
			/// </remarks>
			public void LogEvents(LoggingEvent[] events)
			{
				if (events != null)
				{
					foreach(LoggingEvent logEvent in events)
					{
						if (logEvent != null)
						{
							m_repository.Log(logEvent);
						}
					}
				}
			}

			#endregion Implementation of IRemoteLoggingSink

			#region Override implementation of MarshalByRefObject

			/// <summary>
			/// Obtains a lifetime service object to control the lifetime 
			/// policy for this instance.
			/// </summary>
			/// <returns>
			/// <c>null</c> to indicate that this instance should live
			/// forever.
			/// </returns>
			public override object InitializeLifetimeService()
			{
				return null;
			}

			#endregion Override implementation of MarshalByRefObject

			#region Private Instance Fields

			/// <summary>
			/// The underlying <see cref="ILoggerRepository" /> that events should
			/// be logged to.
			/// </summary>
			private readonly ILoggerRepository m_repository;

			#endregion Private Instance Fields
		}
	}
}

#endif // !NETCF