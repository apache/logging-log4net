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
		/// <param name="sinkUri">The name to publish the sink under in the remoting infrastructure.</param>
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
		public class RemoteLoggingSinkImpl : MarshalByRefObject, IRemoteLoggingSink
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

			#region Protected Instance Properties

			/// <summary>
			/// Gets or sets the underlying <see cref="ILoggerRepository" /> that 
			/// events should be logged to.
			/// </summary>
			/// <value>
			/// The underlying <see cref="ILoggerRepository" /> that events should
			/// be logged to.
			/// </value>
			protected ILoggerRepository LoggerRepository 
			{
				get { return this.m_repository; }
				set { this.m_repository = value; }
			}

			#endregion Protected Instance Properties

			#region Implementation of IRemoteLoggingSink

			/// <summary>
			/// Logs the events to the repository.
			/// </summary>
			/// <param name="events">The events to log.</param>
			/// <remarks>
			/// The events passed are logged to the <see cref="LoggerRepository"/>
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
			private ILoggerRepository m_repository;

			#endregion Private Instance Fields
		}
	}
}
