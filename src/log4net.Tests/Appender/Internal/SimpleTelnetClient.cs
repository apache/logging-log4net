using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace log4net.Tests.Appender.Internal
{
  internal sealed class SimpleTelnetClient(
    Action<string> received, TaskCompletionSource<int> taskCompletionSource, int port = 9090): IDisposable
  {
    private readonly TcpClient client = new("localhost", port);

    internal void Run()
    {
      // Get a stream object for reading and writing
      using NetworkStream stream = client.GetStream();

      int i;
      byte[] bytes = new byte[256];

      bool wasResultSet = false;
      // Loop to receive all the data sent by the server 
      while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
      {
        if (!wasResultSet)
        {
          taskCompletionSource.SetResult(0);
          wasResultSet = true;
        }

        string data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
        received(data);
      }
    }

    /// <inheritdoc/>
    public void Dispose() => client.Dispose();
  }
}