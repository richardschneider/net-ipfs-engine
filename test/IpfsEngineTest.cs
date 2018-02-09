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

            await ipfs.StartAsync();
            await ipfs.StopAsync();

            await ipfs.StartAsync();
            ExceptionAssert.Throws<Exception>(() => ipfs.StartAsync().Wait());
            await ipfs.StopAsync();
        }

        [TestMethod]
        public async Task Swarm_Gets_Bootstrap_Peers()
        {
            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();
            try
            {
                var bootPeers = (await ipfs.Bootstrap.ListAsync()).ToArray();
                var knownPeers = ipfs.SwarmService.KnownPeerAddresses.ToArray();
                while (bootPeers.Count() != knownPeers.Count())
                {
                    await Task.Delay(50);
                    knownPeers = ipfs.SwarmService.KnownPeerAddresses.ToArray();
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
