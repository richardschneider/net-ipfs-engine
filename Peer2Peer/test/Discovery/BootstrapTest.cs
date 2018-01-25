using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer.Discovery
{
    
    [TestClass]
    public class BootstrapTest
    {
        [TestMethod]
        public async Task NullList()
        {
            var bootstrap = new Bootstrap { Addresses = null };
            int found = 0;
            bootstrap.PeerDiscovered += (s, e) =>
            {
                ++found;
            };
            await bootstrap.StartAsync();
            Assert.AreEqual(0, found);
        }

        [TestMethod]
        public async Task Discovered()
        {
            var bootstrap = new Bootstrap
            {
                Addresses = new MultiAddress[]
                {
                    "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
                }
            };
            int found = 0;
            bootstrap.PeerDiscovered += (s, e) =>
            {
                Assert.IsNotNull(e);
                Assert.IsNotNull(e.Address);
                Assert.AreEqual(bootstrap.Addresses.First(), e.Address);
                ++found;
            };
            await bootstrap.StartAsync();
            Assert.AreEqual(1, found);
        }

        [TestMethod]
        public async Task Missing_ID_Is_Ignored()
        {
            var bootstrap = new Bootstrap
            {
                Addresses = new MultiAddress[]
                {
                    "/ip4/104.131.131.82/tcp/4002",
                    "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
                }
            };
            int found = 0;
            bootstrap.PeerDiscovered += (s, e) =>
            {
                Assert.IsNotNull(e);
                Assert.IsNotNull(e.Address);
                Assert.AreEqual(bootstrap.Addresses.Last(), e.Address);
                ++found;
            };
            await bootstrap.StartAsync();
            Assert.AreEqual(1, found);
        }
    }
}
