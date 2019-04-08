using Ipfs;
using Makaretu.Dns;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Discovery
{

    [TestClass]
    public class MdnsTest
    {
        [TestMethod]
        public async Task DiscoveryNext()
        {
            var serviceName = $"_{Guid.NewGuid()}._udp";
            var peer1 = new Peer
            {
                Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ",
                Addresses = new MultiAddress[] { "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ" }
            };
            var peer2 = new Peer
            {
                Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuK",
                Addresses = new MultiAddress[] { "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuK" }
            };
            var done = new ManualResetEvent(false);
            var mdns1 = new MdnsNext
            {
                MulticastService = new MulticastService(),
                ServiceName = serviceName,
                LocalPeer = peer1
            };
            var mdns2 = new MdnsNext
            {
                MulticastService = new MulticastService(),
                ServiceName = serviceName,
                LocalPeer = peer2
            };
            mdns1.PeerDiscovered += (s, e) =>
            {
                if (e.Address.PeerId == peer2.Id)
                    done.Set();
            };
            await mdns1.StartAsync();
            mdns1.MulticastService.Start();
            await mdns2.StartAsync();
            mdns2.MulticastService.Start();
            try
            {
                Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(2)), "timeout");
            }
            finally
            {
                await mdns1.StopAsync();
                await mdns2.StopAsync();
                mdns1.MulticastService.Stop();
                mdns2.MulticastService.Stop();
            }
        }

        [TestMethod]
        public async Task DiscoveryJs()
        {
            var serviceName = $"_{Guid.NewGuid()}._udp";
            serviceName = "_foo._udp";
            var peer1 = new Peer
            {
                Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ",
                Addresses = new MultiAddress[] { "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ" }
            };
            var peer2 = new Peer
            {
                Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuK",
                Addresses = new MultiAddress[] { "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuK" }
            };
            var done = new ManualResetEvent(false);
            var mdns1 = new MdnsJs
            {
                MulticastService = new MulticastService(),
                ServiceName = serviceName,
                LocalPeer = peer1
            };
            var mdns2 = new MdnsJs
            {
                MulticastService = new MulticastService(),
                ServiceName = serviceName,
                LocalPeer = peer2
            };
            mdns1.PeerDiscovered += (s, e) =>
            {
                if (e.Address.PeerId == peer2.Id)
                    done.Set();
            };
            await mdns1.StartAsync();
            mdns1.MulticastService.Start();
            await mdns2.StartAsync();
            mdns2.MulticastService.Start();
            try
            {
                Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(2)), "timeout");
            }
            finally
            {
                await mdns1.StopAsync();
                await mdns2.StopAsync();
                mdns1.MulticastService.Stop();
                mdns2.MulticastService.Stop();
            }
        }

    }
}
