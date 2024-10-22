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

namespace log4net.Appender;

/// <summary>
/// Appender that allows clients to connect via Telnet to receive log messages
/// </summary>
/// <remarks>  
/// <para>
/// The TelnetAppender accepts socket connections and streams logging messages back to the client.
/// The output is provided in a telnet-friendly way so that a log can be monitored over a TCP/IP socket.
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
  private SocketHandler? _handler;
  private int _listeningPort = 23;

  /// <summary>
  /// The fully qualified type of the TelnetAppender class.
  /// </summary>
  /// <remarks>
  /// Used by the internal logger to record the Type of the
  /// log message.
  /// </remarks>
  private static readonly Type _declaringType = typeof(TelnetAppender);

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
    get => _listeningPort;
    set
    {
      if (value is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
      {
        throw SystemInfo.CreateArgumentOutOfRangeException(nameof(value), value,
          $"The value specified for Port is less than {IPEndPoint.MinPort} or greater than {IPEndPoint.MaxPort}.");
      }
      _listeningPort = value;
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

    _handler?.Dispose();
    _handler = null;
  }

  /// <summary>
  /// This appender requires a <see cref="Layout"/> to be set.
  /// </summary>
  protected override bool RequiresLayout => true;

  /// <summary>
  /// Create the socket handler and wait for connections
  /// </summary>
  public override void ActivateOptions()
  {
    base.ActivateOptions();
    try
    {
      LogLog.Debug(_declaringType, $"Creating SocketHandler to listen on port [{_listeningPort}]");
      _handler = new SocketHandler(_listeningPort);
    }
    catch (Exception ex)
    {
      LogLog.Error(_declaringType, "Failed to create SocketHandler", ex);
      throw;
    }
  }

  /// <summary>
  /// Writes the logging event to each connected client.
  /// </summary>
  /// <param name="loggingEvent">The event to log.</param>
  protected override void Append(LoggingEvent loggingEvent)
  {
    if (_handler is not null && _handler.HasConnections)
    {
      _handler.Send(RenderLoggingEvent(loggingEvent));
    }
  }

  /// <summary>
  /// Helper class to manage connected clients
  /// </summary>
  /// <remarks>
  /// <para>
  /// The SocketHandler class is used to accept connections from clients.
  /// It is threaded so that clients can connect/disconnect asynchronously.
  /// </para>
  /// </remarks>
  protected class SocketHandler : IDisposable
  {
    private const int MaxConnections = 20;

    private readonly Socket _serverSocket;
    private readonly List<SocketClient> _clients = [];
    private readonly object _syncRoot = new();
    private bool _wasDisposed;

    /// <summary>
    /// Class that represents a client connected to this handler
    /// </summary>
    protected class SocketClient : IDisposable
    {
      private readonly Socket _socket;
      private readonly StreamWriter _writer;

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
        _socket = socket;
        try
        {
          _writer = new(new NetworkStream(socket));
        }
        catch (Exception e) when (!e.IsFatal())
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
        _writer.Write(message);
        _writer.Flush();
      }

      /// <summary>
      /// Cleans up the client connection.
      /// </summary>
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly")]
      public void Dispose()
      {
        try
        {
          _writer.Dispose();
        }
        catch (Exception e) when (!e.IsFatal())
        {
          // Ignore
        }

        try
        {
          _socket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception e) when (!e.IsFatal())
        {
          // Ignore
        }

        try
        {
          _socket.Dispose();
        }
        catch (Exception e) when (!e.IsFatal())
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
      _serverSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      _serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
      _serverSocket.Listen(5);
      AcceptConnection();
    }

    private void AcceptConnection() => _serverSocket.BeginAccept(OnConnect, null);

    /// <summary>
    /// Sends a string message to each of the connected clients.
    /// </summary>
    /// <param name="message">the text to send</param>
    public void Send(string message)
    {
      List<SocketClient> localClients;
      lock (_syncRoot)
      {
        localClients = _clients.ToList();
      }

      // Send outside lock.
      foreach (SocketClient client in localClients)
      {
        try
        {
          client.Send(message);
        }
        catch (Exception e) when (!e.IsFatal())
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
      lock (_syncRoot)
      {
        _clients.Add(client);
      }
    }

    /// <summary>
    /// Remove a client from the internal clients list
    /// </summary>
    /// <param name="client">client to remove</param>
    private void RemoveClient(SocketClient client)
    {
      lock (_syncRoot)
      {
        _clients.Remove(client);
      }
    }

    /// <summary>
    /// Test if this handler has active connections
    /// </summary>
    public bool HasConnections => _clients.Count > 0;
    // clients.Count is an atomic read that can be done outside the lock.

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
      if (_wasDisposed)
      {
        return;
      }

      try
      {
        // Block until a client connects
        Socket socket = _serverSocket.EndAccept(asyncResult);
        LogLog.Debug(_declaringType, $"Accepting connection from [{socket.RemoteEndPoint}]");
        SocketClient client = new(socket);

        // clients.Count is an atomic read that can be done outside the lock.
        int currentActiveConnectionsCount = _clients.Count;
        if (currentActiveConnectionsCount < MaxConnections)
        {
          try
          {
            client.Send($"TelnetAppender v1.0 ({currentActiveConnectionsCount + 1} active connections)\r\n\r\n");
            AddClient(client);
          }
          catch (Exception e) when (!e.IsFatal())
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
      catch (Exception e) when (!e.IsFatal())
      {
        // Ignore
      }
      finally
      {
        if (!_wasDisposed)
        {
          AcceptConnection();
        }
      }
    }

    /// <summary>
    /// Closes all network connections
    /// </summary>
    public void Dispose()
    {
      if (_wasDisposed)
      {
        return;
      }

      _wasDisposed = true;

      lock (_syncRoot)
      {
        foreach (SocketClient client in _clients)
        {
          client.Dispose();
        }
        _clients.Clear();

        try
        {
          _serverSocket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception e) when (!e.IsFatal())
        {
          // Ignore
        }

        try
        {
          _serverSocket.Dispose();
        }
        catch (Exception e) when (!e.IsFatal())
        {
          // Ignore
        }
      }
    }
  }
}