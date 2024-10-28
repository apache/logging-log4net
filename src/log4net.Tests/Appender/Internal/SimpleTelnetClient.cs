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

  /// <summary>
  /// Runs the client (in a task)
  /// </summary>
  internal void Run() => Task.Run(() =>
  {
    _client.Connect(new IPEndPoint(IPAddress.Loopback, port));
    // Get a stream object for reading and writing
    using NetworkStream stream = _client.GetStream();

    int i;
    byte[] bytes = new byte[256];

    // Loop to receive all the data sent by the server 
    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
    {
      string data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
      received(data);
      if (_cancellationTokenSource.Token.IsCancellationRequested)
      {
        return;
      }
    }
  }, _cancellationTokenSource.Token);

  /// <inheritdoc/>
  public void Dispose()
  {
    _cancellationTokenSource.Cancel();
    _cancellationTokenSource.Dispose();
    _client.Dispose();
  }
}