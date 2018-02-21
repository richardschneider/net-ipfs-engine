using Common.Logging;
using Common.Logging.Simple;
using Ipfs;
using Peer2Peer;
using Peer2Peer.Protocols;
using Peer2Peer.Transports;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalkSpike
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.AutoFlush = true;
            
            // set logger factory
            var properties = new Common.Logging.Configuration.NameValueCollection();
            properties["level"] = "TRACE";
            properties["showLogName"] = "true";
            properties["showDateTime"] = "true";
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);

            // obtain logger instance
            ILog log = LogManager.GetCurrentClassLogger();

            // log something
            log.Debug("Some Debug Log Output");

            //ClientConnect();

            //ServerListen();

            var t = new Test();
            t.SendReceive().Wait();
            Console.WriteLine("finished");
            Console.ReadKey();
        }
        
        static void ServerListen()
        {
            var localEndPoint = new IPEndPoint(IPAddress.Any, 4009);
            Socket listener = new Socket(localEndPoint.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                Console.WriteLine("Got a connection");
                var peer = new NetworkStream(handler);

                Message.WriteAsync("/multistream/1.0.0", peer).Wait();
                Message.ReadStringAsync(peer).Wait();

                while (true)
                {
                    var msg = Message.ReadStringAsync(peer).Result;
                    if (msg == "/mplex/6.7.4")
                    {
                        Message.WriteAsync("na", peer).Wait();
                    }
                    else
                    {
                        Message.WriteAsync(msg, peer).Wait();
                    }
                }
            }
        }

        static void ClientConnect()
        {
            var tcp = new Tcp();
            var peer = tcp.ConnectAsync("/ip4/127.0.0.1/tcp/4002").Result;

            Message.WriteAsync("/multistream/1.0.0", peer).Wait();
            var got = Message.ReadStringAsync(peer).Result;
            Message.WriteAsync("/plaintext/1.0.0", peer).Wait();
            got = Message.ReadStringAsync(peer).Result;

            Message.WriteAsync("/multistream/1.0.0", peer).Wait();
            got = Message.ReadStringAsync(peer).Result;

            //Message.WriteAsync("/ipfs/id/1.0.0", peer).Wait();
            //got = Message.ReadStringAsync(peer).Result;
            //new Identify1().ProcessMessagesAsync(peer).Wait();
            while (true)
            {
                var b = peer.ReadByte();
                if (b == -1) break;
                Console.WriteLine("got {0:x2} '{1}'", b, (char)b);
            }
        }
    }

    class Test
    {
        public async Task SendReceive()
        {
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var tcp = new Tcp();
            using (var server = new HelloServer())
            using (var stream = await tcp.ConnectAsync(server.Address, cs.Token))
            {
                var bytes = new byte[5];
                await stream.ReadAsync(bytes, 0, bytes.Length);
                Console.WriteLine("got " + Encoding.UTF8.GetString(bytes));
                //Assert.AreEqual("hello", Encoding.UTF8.GetString(bytes));
            }
        }

        class HelloServer : IDisposable
        {
            CancellationTokenSource cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            public HelloServer()
            {
                var tcp = new Tcp();
                Address = tcp.Listen("/ip4/127.0.0.1", Handler, cs.Token);
                Console.WriteLine("HelloServer " + Address);
            }

            public MultiAddress Address { get; set; }

            public void Dispose()
            {
                Console.WriteLine("HelloServer: Dispose");
                cs.Cancel();
            }

            void Handler(Stream stream, MultiAddress local, MultiAddress remote)
            {
                var msg = Encoding.UTF8.GetBytes("hello");
                stream.Write(msg, 0, msg.Length);
                stream.Flush();
                stream.Dispose();
            }
        }

    }
}
