using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    [TestClass]
    public class DhtApiTest
    {

        [TestMethod]
        public async Task Local_Info()
        {
            var ipfs = TestFixture.Ipfs;
            var locaId = (await ipfs.LocalPeer).Id;
            var peer = await ipfs.Dht.FindPeerAsync(locaId);

            Assert.IsInstanceOfType(peer, typeof(Peer));
            Assert.AreEqual(locaId, peer.Id);
            Assert.IsNotNull(peer.Addresses);
            Assert.IsTrue(peer.IsValid());
        }

        [TestMethod]
        public async Task Mars_Info()
        {
            var marsId = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var mars = await ipfs.Dht.FindPeerAsync(marsId, cts.Token);
                Assert.AreEqual(marsId, mars.Id);
                Assert.IsNotNull(mars.Addresses);
                Assert.IsTrue(mars.IsValid());
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        [Ignore("https://github.com/richardschneider/net-ipfs-engine/issues/74")]
        public async Task FindProvider()
        {
            var folder = "QmS4ustL54uo8FzR9455qaxZwuMiUhyvMcX9Ba8nUH4uVv";
            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                var providers = await ipfs.Dht.FindProvidersAsync(folder, 1, null, cts.Token);
                Assert.AreEqual(1, providers.Count());
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        public async Task RandomWalk()
        {
            //var id = "QmSoLMeWqB7YGVLJN3pxxQpmmEk35v6wYtsMGLzSr5QBU3"; // TODO
            byte[] x = new byte[32];
            (new Random()).NextBytes(x);
            var id = MultiHash.ComputeHash(x);

            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();
            try
            {
                var prevPeers = (await ipfs.Swarm.AddressesAsync()).ToArray();
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1 * 60));
                await ipfs.Dht.FindPeerAsync(id, cts.Token);
                var peers = (await ipfs.Swarm.AddressesAsync()).ToArray();
                foreach (var peer in peers)
                {
                    if (!prevPeers.Contains(peer))
                    {
                        Console.WriteLine($"found {peer} conn={peer.ConnectedAddress}");
                    }
                }
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }
    }
}

