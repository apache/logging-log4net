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
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;

namespace log4net.Appender.Internal;

/// <summary>
/// Interface for UDP connection management.
/// Only public for unit testing purposes.
/// Do not use outside of log4net.
/// Signatures may change without notice.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IUdpConnection : IDisposable
{
  /// <summary>
  /// Establishes a default remote host using the specified host name and port number.
  /// </summary>
  /// <param name="localPort">The local port number</param>
  /// <param name="host">The remote host to which you intend send data.</param>
  /// <param name="remotePort">The port number on the remote host to which you intend to send data.</param>
  void Connect(int localPort, IPAddress host, int remotePort);

  /// <summary>
  /// Sends a UDP datagram asynchronously to a remote host.
  /// </summary>
  /// <param name="datagram">An array of type System.Byte that specifies the UDP datagram that you intend to send represented as an array of bytes.</param>
  /// <param name="bytes">The number of bytes in the datagram.</param>
  /// <returns>Task for Completion</returns>
  Task<int> SendAsync(byte[] datagram, int bytes);
}