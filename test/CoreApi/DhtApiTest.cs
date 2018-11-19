using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text;
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
                var mars = await ipfs.Dht.FindPeerAsync(marsId);
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
        [Ignore("TODO: way too slow")]
        public async Task FindProvider()
        {
            var folder = "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec";
            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();
            try
            {
                var providers = await ipfs.Dht.FindProvidersAsync(folder, 1);
                Assert.AreEqual(1, providers.Count());
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

    }
}

