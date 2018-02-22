using Ipfs.Engine.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            await ipfs.StartAsync();
            await ipfs.StopAsync();
#if false
            await ipfs.StartAsync();
            await ipfs.StopAsync();

            await ipfs.StartAsync();
            //ExceptionAssert.Throws<Exception>(() => ipfs.StartAsync().Wait());
            await ipfs.StopAsync();
#endif
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
        public async Task Swarm_Gets_Bootstrap_Peers()
        {
            var ipfs = TestFixture.Ipfs;
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
                CollectionAssert.AreEquivalent(bootPeers, knownPeers);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }
    }
}
