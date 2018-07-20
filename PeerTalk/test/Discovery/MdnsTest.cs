using Ipfs;
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
        public async Task Discovery()
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
            var mdns1 = new Mdns
            {
                ServiceName = serviceName,
                LocalPeer = peer1
            };
            var mdns2 = new Mdns
            {
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
            try
            {
                Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(2)), "timeout");
            }
            finally
            {
                await mdns1.StopAsync();
                await mdns2.StopAsync();
            }
        }

    }
}
