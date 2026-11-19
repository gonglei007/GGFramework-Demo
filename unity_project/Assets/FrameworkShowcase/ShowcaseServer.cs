using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GGFramework.Demo
{
    // A real HTTP fixture, bound only to loopback and an OS-assigned free port.
    // No HttpListener URL reservations, Python installation or public server required.
    public sealed class ShowcaseServer : IDisposable
    {
        private readonly TcpListener listener;
        private readonly Thread worker;
        private volatile bool stopped;
        public string Address { get; private set; }

        public ShowcaseServer()
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Address = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/";
            worker = new Thread(Serve) { IsBackground = true, Name = "Showcase HTTP" };
            worker.Start();
        }

        private void Serve()
        {
            while (!stopped)
            {
                try
                {
                    using (var client = listener.AcceptTcpClient())
                    {
                        client.ReceiveTimeout = 1500;
                        client.SendTimeout = 1500;
                        using (var stream = client.GetStream())
                        using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
                        {
                            string first = reader.ReadLine() ?? "";
                            string header;
                            int length = 0;
                            while (!string.IsNullOrEmpty(header = reader.ReadLine()))
                                if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                                    int.TryParse(header.Substring(15).Trim(), out length);
                            if (length > 65536) throw new IOException("Fixture request too large.");
                            // Demo sends ASCII JSON; drain its body before closing the connection.
                            for (int i = 0; i < length; i++) if (reader.Read() < 0) break;
                            bool error = first.Contains("/unavailable");
                            bool business = first.Contains("/business-error");
                            string body = error ? "{\"code\":503,\"msg\":\"Fixture unavailable; cancel or retry\"}"
                                : business ? "{\"code\":409,\"msg\":\"Demo inventory is full\"}"
                                : "{\"code\":200,\"msg\":\"Loopback request succeeded\",\"coins\":128}";
                            byte[] bytes = Encoding.UTF8.GetBytes(body);
                            byte[] headers = Encoding.ASCII.GetBytes("HTTP/1.1 " + (error ? "503 Service Unavailable" : "200 OK")
                                + "\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: " + bytes.Length
                                + "\r\nConnection: close\r\n\r\n");
                            stream.Write(headers, 0, headers.Length);
                            stream.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
                catch (Exception e) when (e is IOException || e is SocketException || e is ObjectDisposedException)
                {
                    // A canceled request or listener shutdown is normal for this fixture.
                }
            }
        }

        public void Dispose()
        {
            stopped = true;
            listener.Stop();
            if (worker.IsAlive) worker.Join(2000);
        }
    }
}
