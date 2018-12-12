using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{

    [TestClass]
    public class SwarmApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;
        MultiAddress somewhere = "/ip4/127.0.0.1";

        [TestMethod]
        public async Task Filter_Add_Remove()
        {
            var addr = await ipfs.Swarm.AddAddressFilterAsync(somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual(somewhere, addr);
            var addrs = await ipfs.Swarm.ListAddressFiltersAsync();
            Assert.IsTrue(addrs.Any(a => a == somewhere));

            addr = await ipfs.Swarm.RemoveAddressFilterAsync(somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual(somewhere, addr);
            addrs = await ipfs.Swarm.ListAddressFiltersAsync();
            Assert.IsFalse(addrs.Any(a => a == somewhere));
        }

        [TestMethod]
        public async Task Connect_Disconnect_Mars()
        {
            var mars = "/dns/mars.i.ipfs.io/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ";
            await ipfs.StartAsync();
            try
            {
                await ipfs.Swarm.ConnectAsync(mars);
                var peers = await ipfs.Swarm.PeersAsync();
                Console.WriteLine($"{peers.Count()} peers");
                foreach (var p in peers)
                    Console.WriteLine($"  {p}");

                Assert.IsTrue(peers.Any(p => p.Id == "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"));
                await ipfs.Swarm.DisconnectAsync(mars);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        [Ignore("TODO: Move to interop tests")]
        public async Task JsIPFS_Connect()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            var remoteAddress = $"/ip4/127.0.0.1/tcp/4002/ipfs/{remoteId}";

            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
            await ipfs.Swarm.ConnectAsync(remoteAddress, cts.Token);
            Assert.AreEqual(1, (await ipfs.Swarm.PeersAsync()).Count());

            await ipfs.Swarm.DisconnectAsync(remoteAddress);
            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
        }

        [TestMethod]
        [Ignore("TODO: Move to interop tests")]
        public async Task GoIPFS_Connect()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var remoteId = "QmdoxrwszT6b9srLXHYBPFVRXmZSFAosWLXoQS9TEEAaix";
            var remoteAddress = $"/ip4/127.0.0.1/tcp/4001/ipfs/{remoteId}";

            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
            await ipfs.Swarm.ConnectAsync(remoteAddress, cts.Token);
            Assert.AreEqual(1, (await ipfs.Swarm.PeersAsync()).Count());

            await ipfs.Swarm.DisconnectAsync(remoteAddress);
            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
        }

        [TestMethod]
        [Ignore("TODO: Move to interop tests")]
        public async Task GoIPFS_Connect_v0_4_17()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var remoteId = "QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd";
            var remoteAddress = $"/ip4/178.62.158.247/tcp/4001/ipfs/{remoteId}";

            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
            await ipfs.Swarm.ConnectAsync(remoteAddress, cts.Token);
            Assert.AreEqual(1, (await ipfs.Swarm.PeersAsync()).Count());

            await ipfs.Swarm.DisconnectAsync(remoteAddress);
            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
        }

    }
}
