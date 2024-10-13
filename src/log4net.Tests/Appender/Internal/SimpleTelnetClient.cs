using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace log4net.Tests.Appender.Internal
{
  internal sealed class SimpleTelnetClient(Action<string> received, int port = 9090)
  {
    internal void Run()
    {
      TcpClient client = new("localhost", port);
      try
      {
        // Get a stream object for reading and writing
        using NetworkStream stream = client.GetStream();

        int i;
        byte[] bytes = new byte[256];

        // Loop to receive all the data sent by the server 
        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
        {
          string data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
          received(data);
        }
      }
      finally
      {
        client.Dispose();
      }
    }
  }
}