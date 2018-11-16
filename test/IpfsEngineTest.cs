using Ipfs.Engine.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    
    [TestClass]
    public class IpfsEngineTest
    {
        [TestMethod]
        public void Can_Create()
        {
            var ipfs = new IpfsEngine("this is not a secure pass phrase".ToCharArray());
            Assert.IsNotNull(ipfs);
        }

        [TestMethod]
        public async Task Wrong_Passphrase()
        {
            var ipfs1 = TestFixture.Ipfs;
            await ipfs1.KeyChain();

            var ipfs2 = new IpfsEngine("the wrong pass phrase".ToCharArray())
            {
                Options = ipfs1.Options
            };
            ExceptionAssert.Throws<UnauthorizedAccessException>(() =>
            {
                var _ = ipfs2.KeyChain().Result;
            });
        }

        [TestMethod]
        public async Task Can_Start_And_Stop()
        {
            var ipfs = TestFixture.Ipfs;
            var peer = await ipfs.LocalPeer;

            await ipfs.StartAsync();
            Assert.AreNotEqual(0, peer.Addresses.Count());
            await ipfs.StopAsync();
            Assert.AreEqual(0, peer.Addresses.Count());

            await ipfs.StartAsync();
            Assert.AreNotEqual(0, peer.Addresses.Count());
            await ipfs.StopAsync();
            Assert.AreEqual(0, peer.Addresses.Count());

            await ipfs.StartAsync();
            Assert.AreNotEqual(0, peer.Addresses.Count());
            ExceptionAssert.Throws<Exception>(() => ipfs.StartAsync().Wait());
            await ipfs.StopAsync();
            Assert.AreEqual(0, peer.Addresses.Count());
        }

        [TestMethod]
        public async Task Can_Start_And_Stop_MultipleEngines()
        {
            var ipfs1 = TestFixture.Ipfs;
            var ipfs2 = TestFixture.IpfsOther;
            var peer1 = await ipfs1.LocalPeer;
            var peer2 = await ipfs2.LocalPeer;

            for (int n = 0; n < 3; ++n)
            {
                await ipfs1.StartAsync();
                Assert.AreNotEqual(0, peer1.Addresses.Count());
                await ipfs2.StartAsync();
                Assert.AreNotEqual(0, peer2.Addresses.Count());

                await ipfs2.StopAsync();
                Assert.AreEqual(0, peer2.Addresses.Count());
                await ipfs1.StopAsync();
                Assert.AreEqual(0, peer1.Addresses.Count());
            }
        }

        [TestMethod]
        public async Task LocalPeer()
        {
            var ipfs = TestFixture.Ipfs;
            Task<Peer>[] tasks = new Task<Peer>[]
            {
                Task.Run(async () => await ipfs.LocalPeer),
                Task.Run(async () => await ipfs.LocalPeer)
            };
            var r = await Task.WhenAll(tasks);
            Assert.AreSame(r[0], r[1]);
        }

        [TestMethod]
        public async Task KeyChain()
        {
            var ipfs = TestFixture.Ipfs;
            Task<KeyChain>[] tasks = new Task<KeyChain>[]
            {
                Task.Run(async () => await ipfs.KeyChain()),
                Task.Run(async () => await ipfs.KeyChain())
            };
            var r = await Task.WhenAll(tasks);
            Assert.AreSame(r[0], r[1]);
        }

        [TestMethod]
        public async Task KeyChain_GetKey()
        {
            var ipfs = TestFixture.Ipfs;
            var keyChain = await ipfs.KeyChain();
            var key = await keyChain.GetPrivateKeyAsync("self");
            Assert.IsNotNull(key);
            Assert.IsTrue(key.IsPrivate);
        }

        [TestMethod]
        public async Task Swarm_Gets_Bootstrap_Peers()
        {
            var ipfs = TestFixture.Ipfs;
            var bootPeers = (await ipfs.Bootstrap.ListAsync()).ToArray();
            await ipfs.StartAsync();
            try
            {
                var swarm = await ipfs.SwarmService;
                var knownPeers = swarm.KnownPeerAddresses.ToArray();
                var endTime = DateTime.Now.AddSeconds(3);
                while (true)
                {
                    if (DateTime.Now > endTime)
                        Assert.Fail("Bootstrap peers are not known.");
                    if (bootPeers.All(a => knownPeers.Contains(a)))
                        break;

                    await Task.Delay(50);
                    knownPeers = swarm.KnownPeerAddresses.ToArray();
                }
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        public async Task Start_NoListeners()
        {
            var ipfs = TestFixture.Ipfs;
            var swarm = await ipfs.Config.GetAsync("Addresses.Swarm");
            try
            {
                await ipfs.Config.SetAsync("Addresses.Swarm", "[]");
                await ipfs.StartAsync();
            }
            finally
            {
                await ipfs.StopAsync();
                await ipfs.Config.SetAsync("Addresses.Swarm", swarm);
            }
        }

        [TestMethod]
        public async Task Start_InvalidListener()
        {
            var ipfs = TestFixture.Ipfs;
            var swarm = await ipfs.Config.GetAsync("Addresses.Swarm");
            try
            {
                // 1 - missing ip address
                // 2 - invalid protocol name
                // 3 - okay
                var values = JToken.Parse("['/tcp/0', '/foo/bar', '/ip4/0.0.0.0/tcp/0']");
                await ipfs.Config.SetAsync("Addresses.Swarm", values);
                await ipfs.StartAsync();
            }
            finally
            {
                await ipfs.StopAsync();
                await ipfs.Config.SetAsync("Addresses.Swarm", swarm);
            }
        }
    }
}
