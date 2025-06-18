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

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using log4net.Util;

namespace log4net.Appender.Internal;

/// <summary>
/// Wrapper for <see cref="UdpClient"/> to manage UDP connections.
/// </summary>
internal sealed class UdpConnection : IUdpConnection
{
  private UdpClient? _client;

  private UdpClient Client 
    => _client.EnsureNotNull(errorMessage: "Client is not initialized. Call Connect first.");

  /// <inheritdoc/>
  public void Connect(int localPort, IPAddress remoteAddress, int remotePort)
  {
    _client = CreateClient(localPort, remoteAddress);
    Client.Connect(remoteAddress, remotePort);
  }

  /// <inheritdoc/>
  public Task<int> SendAsync(byte[] datagram, int bytes) => Client.SendAsync(datagram, bytes);

  /// <inheritdoc/>
  public void Dispose() => _client?.Dispose();

  /// <summary>
  /// Creates a new <see cref="UdpClient"/> instance configured with the specified local port and remote address.
  /// </summary>
  /// <returns>A <see cref="UdpClient"/> instance configured with the specified parameters.</returns>
  internal static UdpClient CreateClient(int localPort, System.Net.IPAddress remoteAddress)
    => localPort == 0
      ? new (remoteAddress.AddressFamily)
      : new (localPort, remoteAddress.AddressFamily);
}