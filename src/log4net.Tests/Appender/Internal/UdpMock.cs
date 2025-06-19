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

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using log4net.Appender.Internal;

namespace log4net.Tests.Appender.Internal;

/// <summary>
/// Mock implementation of <see cref="IUdpConnection"/> for testing purposes.
/// </summary>
internal sealed class UdpMock : IUdpConnection
{
  /// <summary>
  /// Passed to <see cref="SendAsync(byte[], int)"/>
  /// </summary>
  public List<(byte[] Datagram, int Bytes)> Sent { get; } = [];

  /// <summary>
  /// Was <see cref="Dispose"/> called
  /// </summary>
  internal bool WasDisposed { get; private set; }

  /// <summary>
  /// Parameters passed to <see cref="Connect(int, IPAddress, int)"/>
  /// </summary>
  internal (int LocalPort, IPAddress Host, int RemotePort)? ConnectedTo { get; private set; }

  /// <inheritdoc/>
  public void Connect(int localPort, IPAddress host, int remotePort) 
    => ConnectedTo = (localPort, host, remotePort);

  /// <inheritdoc/>
  public void Dispose() => WasDisposed = true;

  /// <inheritdoc/>
  public Task<int> SendAsync(byte[] datagram, int bytes)
  {
    Sent.Add((datagram, bytes));
    return Task.FromResult(bytes);
  }
}