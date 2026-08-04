/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace log4net.Tests.Appender.Internal;

/// <summary>
/// Telnet Client for unit testing
/// </summary>
/// <param name="received">Callback for received messages</param>
/// <param name="port">TCP-Port to use</param>
internal sealed class SimpleTelnetClient(
  Action<string> received, int port) : IDisposable
{
  private readonly CancellationTokenSource _cancellationTokenSource = new();
  private readonly TcpClient _client = new();
  private volatile bool _disposing;

  /// <summary>
  /// Runs the client (in a task)
  /// </summary>
  /// <param name="log">Callback for unexpected errors - a passing run stays silent</param>
  internal void Run(Action<string> log) => Task.Run(() =>
  {
    try
    {
      _client.Connect(new IPEndPoint(IPAddress.Loopback, port));
      // Get a stream object for reading and writing
      using NetworkStream stream = _client.GetStream();

      int i;
      byte[] bytes = new byte[256];

      // Loop to receive all the data sent by the server. Dispose shuts the socket down,
      // which ends the stream, so this read returns 0 and the loop exits without throwing.
      while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
      {
        received(System.Text.Encoding.ASCII.GetString(bytes, 0, i));
        if (_cancellationTokenSource.Token.IsCancellationRequested)
        {
          return;
        }
      }
    }
    // The test asserts on the received data, so a failing client must not end up
    // as an unobserved task exception - log it instead. Anything thrown once Dispose
    // has started is teardown noise, so only genuine failures reach the output.
    catch (SocketException e)
    {
      Report(e);
    }
    catch (IOException e)
    {
      Report(e);
    }
    catch (ObjectDisposedException e)
    {
      Report(e);
    }

    void Report(Exception e)
    {
      if (!_disposing)
      {
        log("client: error: " + e);
      }
    }
  }, _cancellationTokenSource.Token);

  /// <inheritdoc/>
  public void Dispose()
  {
    _disposing = true;
    _cancellationTokenSource.Cancel();
    // Shut the socket down before disposing it: that ends the stream cleanly, so a read
    // blocked in Run returns 0 instead of failing with a connection abort.
    try
    {
      _client.Client?.Shutdown(SocketShutdown.Both);
    }
    catch (SocketException)
    {
      // not connected - nothing to shut down
    }
    catch (ObjectDisposedException)
    {
      // already disposed - nothing to shut down
    }
    _cancellationTokenSource.Dispose();
    _client.Dispose();
  }
}