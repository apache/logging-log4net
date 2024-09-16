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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using log4net.Core;
using log4net.Util;

namespace log4net.Appender
{
  /// <summary>
  /// Appender that allows clients to connect via Telnet to receive log messages
  /// </summary>
  /// <remarks>  
  /// <para>
  /// The TelnetAppender accepts socket connections and streams logging messages
  /// back to the client.  
  /// The output is provided in a telnet-friendly way so that a log can be monitored 
  /// over a TCP/IP socket.
  /// This allows simple remote monitoring of application logging.
  /// </para>
  /// <para>
  /// The default <see cref="Port"/> is 23 (the telnet port).
  /// </para>
  /// </remarks>
  /// <author>Keith Long</author>
  /// <author>Nicko Cadell</author>
  public class TelnetAppender : AppenderSkeleton
  {
    private SocketHandler? m_handler;
    private int m_listeningPort = 23;

    /// <summary>
    /// The fully qualified type of the TelnetAppender class.
    /// </summary>
    /// <remarks>
    /// Used by the internal logger to record the Type of the
    /// log message.
    /// </remarks>
    private static readonly Type declaringType = typeof(TelnetAppender);

    /// <summary>
    /// Gets or sets the TCP port number on which this <see cref="TelnetAppender"/> will listen for connections.
    /// </summary>
    /// <value>
    /// An integer value in the range <see cref="IPEndPoint.MinPort" /> to <see cref="IPEndPoint.MaxPort" /> 
    /// indicating the TCP port number on which this <see cref="TelnetAppender"/> will listen for connections.
    /// </value>
    /// <remarks>
    /// <para>
    /// The default value is 23 (the telnet port).
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value specified is less than <see cref="IPEndPoint.MinPort" /> 
    /// or greater than <see cref="IPEndPoint.MaxPort" />.</exception>
    public int Port
    {
      get => m_listeningPort;
      set
      {
        if (value < IPEndPoint.MinPort || value > IPEndPoint.MaxPort)
        {
          throw SystemInfo.CreateArgumentOutOfRangeException(nameof(value), value,
            $"The value specified for Port is less than {IPEndPoint.MinPort} or greater than {IPEndPoint.MaxPort}.");
        }
        else
        {
          m_listeningPort = value;
        }
      }
    }

    /// <summary>
    /// Overrides the parent method to close the socket handler
    /// </summary>
    /// <remarks>
    /// <para>
    /// Closes all the outstanding connections.
    /// </para>
    /// </remarks>
    protected override void OnClose()
    {
      base.OnClose();

      if (m_handler is not null)
      {
        m_handler.Dispose();
        m_handler = null;
      }
    }

    /// <summary>
    /// This appender requires a <see cref="Layout"/> to be set.
    /// </summary>
    protected override bool RequiresLayout => true;

    /// <summary>
    /// Initializes the appender based on the options set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is part of the <see cref="IOptionHandler"/> delayed object
    /// activation scheme. The <see cref="ActivateOptions"/> method must 
    /// be called on this object after the configuration properties have
    /// been set. Until <see cref="ActivateOptions"/> is called this
    /// object is in an undefined state and must not be used. 
    /// </para>
    /// <para>
    /// If any of the configuration properties are modified then 
    /// <see cref="ActivateOptions"/> must be called again.
    /// </para>
    /// <para>
    /// Create the socket handler and wait for connections
    /// </para>
    /// </remarks>
    public override void ActivateOptions()
    {
      base.ActivateOptions();
      try
      {
        LogLog.Debug(declaringType, $"Creating SocketHandler to listen on port [{m_listeningPort}]");
        m_handler = new SocketHandler(m_listeningPort);
      }
      catch (Exception ex)
      {
        LogLog.Error(declaringType, "Failed to create SocketHandler", ex);
        throw;
      }
    }

    /// <summary>
    /// Writes the logging event to each connected client.
    /// </summary>
    /// <param name="loggingEvent">The event to log.</param>
    protected override void Append(LoggingEvent loggingEvent)
    {
      if (m_handler is not null && m_handler.HasConnections)
      {
        m_handler.Send(RenderLoggingEvent(loggingEvent));
      }
    }

    /// <summary>
    /// Helper class to manage connected clients
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SocketHandler class is used to accept connections from
    /// clients.  It is threaded so that clients can connect/disconnect
    /// asynchronously.
    /// </para>
    /// </remarks>
    protected class SocketHandler : IDisposable
    {
      private const int MAX_CONNECTIONS = 20;

      private readonly Socket m_serverSocket;
      private readonly List<SocketClient> m_clients = new();
      private readonly object m_lockObj = new();
      private bool m_disposed;

      /// <summary>
      /// Class that represents a client connected to this handler
      /// </summary>
      protected class SocketClient : IDisposable
      {
        private readonly Socket m_socket;
        private readonly StreamWriter m_writer;

        /// <summary>
        /// Create this <see cref="SocketClient"/> for the specified <see cref="Socket"/>
        /// </summary>
        /// <param name="socket">the client's socket</param>
        /// <remarks>
        /// <para>
        /// Opens a stream writer on the socket.
        /// </para>
        /// </remarks>
        public SocketClient(Socket socket)
        {
          m_socket = socket;

          try
          {
            m_writer = new StreamWriter(new NetworkStream(socket));
          }
          catch
          {
            Dispose();
            throw;
          }
        }

        /// <summary>
        /// Writes a string to the client.
        /// </summary>
        /// <param name="message">string to send</param>
        public void Send(string message)
        {
          m_writer.Write(message);
          m_writer.Flush();
        }

        /// <summary>
        /// Cleans up the client connection.
        /// </summary>
        public void Dispose()
        {
          try
          {
            m_writer.Dispose();
          }
          catch
          {
            // Ignore
          }

          try
          {
            m_socket.Shutdown(SocketShutdown.Both);
          }
          catch
          {
            // Ignore
          }

          try
          {
            m_socket.Dispose();
          }
          catch
          {
            // Ignore
          }
        }
      }

      /// <summary>
      /// Opens a new server port on <paramref ref="port"/>
      /// </summary>
      /// <param name="port">the local port to listen on for connections</param>
      /// <remarks>
      /// <para>
      /// Creates a socket handler on the specified local server port.
      /// </para>
      /// </remarks>
      public SocketHandler(int port)
      {
        m_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        m_serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        m_serverSocket.Listen(5);
        AcceptConnection();
      }

      private void AcceptConnection()
      {
        m_serverSocket.BeginAccept(OnConnect, null);
      }

      /// <summary>
      /// Sends a string message to each of the connected clients.
      /// </summary>
      /// <param name="message">the text to send</param>
      public void Send(string message)
      {

        List<SocketClient> localClients;
        lock (m_lockObj)
        {
          localClients = m_clients.ToList();
        }
        
        // Send outside lock.
        foreach (SocketClient client in localClients)
        {
          try
          {
            client.Send(message);
          }
          catch (Exception)
          {
            // The client has closed the connection, remove it from our list
            client.Dispose();
            RemoveClient(client);
          }
        }
      }

      /// <summary>
      /// Add a client to the internal clients list
      /// </summary>
      /// <param name="client">client to add</param>
      private void AddClient(SocketClient client)
      {
        lock (m_lockObj)
        {
          m_clients.Add(client);
        }
      }

      /// <summary>
      /// Remove a client from the internal clients list
      /// </summary>
      /// <param name="client">client to remove</param>
      private void RemoveClient(SocketClient client)
      {
        lock (m_lockObj)
        {
          m_clients.Remove(client);
        }
      }

      /// <summary>
      /// Test if this handler has active connections
      /// </summary>
      /// <value>
      /// <c>true</c> if this handler has active connections
      /// </value>
      /// <remarks>
      /// <para>
      /// This property will be <c>true</c> while this handler has
      /// active connections, that is at least one connection that 
      /// the handler will attempt to send a message to.
      /// </para>
      /// </remarks>
      public bool HasConnections
      {
        get
        {
          // m_clients.Count is an atomic read that can be done outside the lock.
          return m_clients.Count > 0;
        }
      }

      /// <summary>
      /// Callback used to accept a connection on the server socket
      /// </summary>
      /// <param name="asyncResult">The result of the asynchronous operation</param>
      /// <remarks>
      /// <para>
      /// On connection adds to the list of connections 
      /// if there are too many open connections you will be disconnected
      /// </para>
      /// </remarks>
      private void OnConnect(IAsyncResult asyncResult)
      {
        try
        {
          // Block until a client connects
          Socket socket = m_serverSocket.EndAccept(asyncResult);
          LogLog.Debug(declaringType, $"Accepting connection from [{socket.RemoteEndPoint}]");
          var client = new SocketClient(socket);

          // m_clients.Count is an atomic read that can be done outside the lock.
          int currentActiveConnectionsCount = m_clients.Count;
          if (currentActiveConnectionsCount < MAX_CONNECTIONS)
          {
            try
            {
              client.Send($"TelnetAppender v1.0 ({(currentActiveConnectionsCount + 1)} active connections)\r\n\r\n");
              AddClient(client);
            }
            catch
            {
              client.Dispose();
            }
          }
          else
          {
            client.Send("Sorry - Too many connections.\r\n");
            client.Dispose();
          }
        }
        catch
        {
          // Ignore
        }
        finally
        {
          AcceptConnection();
        }
      }

      /// <summary>
      /// Closes all network connections
      /// </summary>
      public void Dispose()
      {
        if (m_disposed)
        {
          return;
        }

        m_disposed = true;

        lock (m_lockObj)
        {
          foreach (SocketClient client in m_clients)
          {
            client.Dispose();
          }
          m_clients.Clear();

          try
          {
            m_serverSocket.Shutdown(SocketShutdown.Both);
          }
          catch
          {
            // Ignore
          }

          try
          {
            m_serverSocket.Dispose();
          }
          catch
          {
            // Ignore
          }
        }
      }
    }
  }
}
