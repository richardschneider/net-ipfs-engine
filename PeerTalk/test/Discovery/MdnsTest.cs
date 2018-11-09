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
            var multicastService = new MulticastService();

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
                MulticastService = multicastService,
                ServiceName = serviceName,
                LocalPeer = peer1
            };
            var mdns2 = new MdnsNext
            {
                MulticastService = multicastService,
                ServiceName = serviceName,
                LocalPeer = peer2
            };
            mdns1.PeerDiscovered += (s, e) =>
            {
                if (e.Address.PeerId == peer2.Id)
                    done.Set();
            };
            await mdns1.StartAsync();
            await mdns2.StartAsync();
            multicastService.Start();
            try
            {
                Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(2)), "timeout");
            }
            finally
            {
                await mdns1.StopAsync();
                await mdns2.StopAsync();
                multicastService.Stop();
            }
        }

        [TestMethod]
        public async Task DiscoveryJs()
        {
            var multicastService = new MulticastService();

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
                MulticastService = multicastService,
                ServiceName = serviceName,
                LocalPeer = peer1
            };
            var mdns2 = new MdnsJs
            {
                MulticastService = multicastService,
                ServiceName = serviceName,
                LocalPeer = peer2
            };
            mdns1.PeerDiscovered += (s, e) =>
            {
                if (e.Address.PeerId == peer2.Id)
                    done.Set();
            };
            await mdns1.StartAsync();
            await mdns2.StartAsync();
            multicastService.Start();
            try
            {
                Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(2)), "timeout");
            }
            finally
            {
                await mdns1.StopAsync();
                await mdns2.StopAsync();
                multicastService.Stop();
            }
        }

    }
}
