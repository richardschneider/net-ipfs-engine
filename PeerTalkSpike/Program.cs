using Common.Logging;
using Common.Logging.Simple;
using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Engine;
using PeerTalk;
using PeerTalk.Protocols;
using PeerTalk.Transports;
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

            ClientConnect();

            //ServerListen();

            var t = new Test();
            //t.Chunking().Wait();
            //log.Debug("--- RUN Can_Start_And_Stop");
            //t.Can_Start_And_Stop().Wait();
            //log.Debug("--- RUN Swarm_Gets_Bootstrap_Peers");
            //t.Swarm_Gets_Bootstrap_Peers().Wait();
            //Console.WriteLine("finished");
            //Console.ReadKey();
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
        const string passphrase = "this is not a secure pass phrase";
        public static IpfsEngine ipfs = new IpfsEngine(passphrase.ToCharArray());

        static Test()
        {
            ipfs.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), "ipfs-test4");
        }
        public async Task Can_Start_And_Stop()
        {
            await ipfs.StartAsync();
            //await Task.Delay(1000);
            await ipfs.StopAsync();
#if false
            await ipfs.StartAsync();
            await ipfs.StopAsync();

            await ipfs.StartAsync();
            //ExceptionAssert.Throws<Exception>(() => ipfs.StartAsync().Wait());
            await ipfs.StopAsync();
#endif
        }
        public async Task Swarm_Gets_Bootstrap_Peers()
        {
            var bootPeers = (await ipfs.Bootstrap.ListAsync()).ToArray();
            await ipfs.StartAsync();
            try
            {
                var swarm = await ipfs.SwarmService;
                var knownPeers = swarm.KnownPeerAddresses.ToArray();
                while (bootPeers.Count() != knownPeers.Count())
                {
                    await Task.Delay(50);
                    knownPeers = swarm.KnownPeerAddresses.ToArray();
                }
                //CollectionAssert.AreEquivalent(bootPeers, knownPeers);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }
        public async Task Chunking()
        {
            var options = new AddFileOptions
            {
                ChunkSize = 3
            };
            options.Pin = true;
            var node = await ipfs.FileSystem.AddTextAsync("hello world", options);
            var links = node.Links.ToArray();
            //Assert.AreEqual("QmVVZXWrYzATQdsKWM4knbuH5dgHFmrRqW3nJfDgdWrBjn", (string)node.Id);
            //Assert.AreEqual(false, node.IsDirectory);
            //Assert.AreEqual(4, links.Length);
            //Assert.AreEqual("QmevnC4UDUWzJYAQtUSQw4ekUdqDqwcKothjcobE7byeb6", (string)links[0].Id);
            //Assert.AreEqual("QmTdBogNFkzUTSnEBQkWzJfQoiWbckLrTFVDHFRKFf6dcN", (string)links[1].Id);
            //Assert.AreEqual("QmPdmF1n4di6UwsLgW96qtTXUsPkCLN4LycjEUdH9977d6", (string)links[2].Id);
            //Assert.AreEqual("QmXh5UucsqF8XXM8UYQK9fHXsthSEfi78kewr8ttpPaLRE", (string)links[3].Id);

            var text = await ipfs.FileSystem.ReadAllTextAsync(node.Id);
            //Assert.AreEqual("hello world", text);
        }

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
