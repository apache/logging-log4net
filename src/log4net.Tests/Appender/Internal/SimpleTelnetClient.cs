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