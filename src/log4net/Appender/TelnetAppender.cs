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
using System.Text;
using System.IO;
using System.Linq;
using log4net.Appender.Internal;
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
/// </para>
/// <para>
/// The default <see cref="Port"/> is 23 (the telnet port) and the default
/// <see cref="ListenAddress"/> is <see cref="IPAddress.Loopback"/>, so monitoring from another
/// machine has to be turned on deliberately.
/// </para>
/// <para>
/// This appender is a diagnostic tool for trusted networks. As with any other appender
/// destination, the connecting client is trusted: enabling the appender declares that whoever can
/// reach the port may read the application's log, so no authentication is performed and the stream
/// is not encrypted. Keeping untrusted parties away from the port is the operator's
/// responsibility, exactly as it is for a log file.
/// </para>
/// </remarks>
/// <author>Keith Long</author>
/// <author>Nicko Cadell</author>
public class TelnetAppender : AppenderSkeleton
{
  private SocketHandler? _handler;
  private int _listeningPort = 23;
  private int _sendTimeoutMillis = 5_000;
  private IPAddress _listenAddress = IPAddress.Loopback;

  /// <summary>
  /// Gets or sets the address to listen on.
  /// </summary>
  /// <value>
  /// The local address to accept connections on. The default is <see cref="IPAddress.Loopback"/>,
  /// the machine the application runs on.
  /// </value>
  /// <remarks>
  /// <para>
  /// The stream is unauthenticated and unencrypted, so reaching it from another machine is opt-in:
  /// set this to <see cref="IPAddress.Any"/> or <see cref="IPAddress.IPv6Any"/> for that, and keep
  /// untrusted parties away from the port.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentNullException">The value specified is <see langword="null"/>.</exception>
  public IPAddress ListenAddress
  {
    get => _listenAddress;
    set => _listenAddress = value.EnsureNotNull();
  }

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
  /// Gets or sets the time, in milliseconds, that a write to a client may block before that
  /// client is treated as dead and disconnected.
  /// </summary>
  /// <value>
  /// A positive number of milliseconds, or 0 to block indefinitely.
  /// </value>
  /// <remarks>
  /// <para>
  /// Clients are written to synchronously while the appender lock is held, so a client that
  /// connects and then stops reading lets TCP flow control fill its receive window and the
  /// server send buffer. Without a timeout the next write blocks forever and suspends every
  /// thread that logs through this appender.
  /// </para>
  /// <para>
  /// The default value is 5000 (5 seconds). A write that exceeds it fails with a
  /// <see cref="SocketException"/>, and the client is then disconnected like any other dead
  /// connection. Setting the value to 0 restores the previous behavior of blocking
  /// indefinitely and is not recommended.
  /// </para>
  /// </remarks>
  /// <exception cref="ArgumentOutOfRangeException">The value specified is negative.</exception>
  public int SendTimeoutMillis
  {
    get => _sendTimeoutMillis;
    set
    {
      if (value < 0)
      {
        throw SystemInfo.CreateArgumentOutOfRangeException(nameof(value), value,
          "The value specified for SendTimeoutMillis is negative.");
      }
      _sendTimeoutMillis = value;
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
      LogLog.Debug(_declaringType, $"Creating SocketHandler to listen on [{_listenAddress}]:[{_listeningPort}]");
      _handler = new(_listenAddress, _listeningPort, _sendTimeoutMillis);
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
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly")]
  protected class SocketHandler : IDisposable
  {
    private const int MaxConnections = 20;

    private readonly Socket _serverSocket;
    private readonly int _sendTimeoutMillis;
    private readonly List<SocketClient> _clients = [];
    private readonly object _syncRoot = new();
    private bool _wasDisposed;

    /// <summary>
    /// Class that represents a client connected to this handler
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly")]
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
          // Belt and braces. Send escapes what cannot be encoded; this keeps a future gap costing
          // one character rather than every client, since Send reads a throw as a hung up client.
          _writer = new(new NetworkStream(socket), new UTF8Encoding(false));
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
        _writer.Write(ContentEscape.EscapeUnpairedSurrogates(message.EnsureNotNull()));
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
    /// Creates a socket handler on the specified local server port, blocking indefinitely on
    /// clients that stop reading. Prefer <see cref="SocketHandler(int, int)"/>.
    /// </para>
    /// </remarks>
    public SocketHandler(int port)
      : this(port, 0)
    { }

    /// <summary>
    /// Opens a new server port on <paramref ref="port"/>
    /// </summary>
    /// <param name="port">the local port to listen on for connections</param>
    /// <param name="sendTimeoutMillis">the time, in milliseconds, that a write to a client may
    /// block before that client is disconnected, or 0 to block indefinitely</param>
    /// <remarks>
    /// <para>
    /// Creates a socket handler on the specified local server port, listening on
    /// <see cref="IPAddress.Loopback"/>.
    /// </para>
    /// </remarks>
    public SocketHandler(int port, int sendTimeoutMillis)
      : this(IPAddress.Loopback, port, sendTimeoutMillis)
    { }

    /// <summary>
    /// Opens a new server port on <paramref ref="port"/> of <paramref ref="listenAddress"/>
    /// </summary>
    /// <param name="listenAddress">the local address to accept connections on</param>
    /// <param name="port">the local port to listen on for connections</param>
    /// <param name="sendTimeoutMillis">the time, in milliseconds, that a write to a client may
    /// block before that client is disconnected, or 0 to block indefinitely</param>
    /// <remarks>
    /// <para>
    /// Creates a socket handler on the specified local address and server port.
    /// </para>
    /// </remarks>
    public SocketHandler(IPAddress listenAddress, int port, int sendTimeoutMillis)
    {
      _sendTimeoutMillis = sendTimeoutMillis;
      // The address decides the family, so that an IPv6 address does not end up on an IPv4 socket.
      _serverSocket = new(listenAddress.EnsureNotNull().AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      _serverSocket.Bind(new IPEndPoint(listenAddress, port));
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
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
        if (_sendTimeoutMillis > 0)
        {
          // Bound how long a write to this client can block. Clients are written to while the
          // appender lock is held, so without a timeout a client that stops reading suspends
          // every thread that logs. A timed-out write throws and the client is then evicted
          // like any other dead connection.
          socket.SendTimeout = _sendTimeoutMillis;
        }
        SocketClient client = new(socket);

        // clients.Count is an atomic read that can be done outside the lock.
        int currentActiveConnectionsCount = _clients.Count;
        if (currentActiveConnectionsCount < MaxConnections)
        {
          // Register the client before sending the welcome message, otherwise a client
          // that logs as soon as it has received the welcome message can race with
          // AddClient and see HasConnections == false.
          AddClient(client);
          try
          {
            client.Send($"TelnetAppender v1.0 ({currentActiveConnectionsCount + 1} active connections)\r\n\r\n");
          }
          catch (Exception e) when (!e.IsFatal())
          {
            RemoveClient(client);
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