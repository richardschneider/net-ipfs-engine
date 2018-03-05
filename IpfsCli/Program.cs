using Common.Logging;
using Common.Logging.Simple;
using Ipfs;
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

namespace Ipfs.Cli
{
    class Program
    {
        const string passphrase = "this is not a secure pass phrase";
        public static IpfsEngine ipfs = new IpfsEngine(passphrase.ToCharArray());

        static void Main(string[] args)
        {
            // set logger factory
            var properties = new Common.Logging.Configuration.NameValueCollection();
            properties["level"] = "TRACE";
            properties["showLogName"] = "true";
            properties["showDateTime"] = "true";
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);

            ipfs.StartAsync().Wait();

            Console.ReadKey();
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
