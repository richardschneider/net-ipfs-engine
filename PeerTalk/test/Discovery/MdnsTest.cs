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
            MultiAddress listen = "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ";
            var done = new ManualResetEvent(false);
            var mdns = new Mdns
            {
                ServiceName = $"{Guid.NewGuid()}.local",
                Addresses = new List<MultiAddress> { listen }
            };
            mdns.PeerDiscovered += (s, e) =>
            {
                if (e.Address == listen)
                    done.Set();
            };
            await mdns.StartAsync();
            try
            {
                Assert.IsTrue(done.WaitOne(TimeSpan.FromSeconds(2)), "timeout");
            }
            finally
            {
                await mdns.StopAsync();
            }
        }

        [TestMethod]
        public async Task NoBroadcast()
        {
            MultiAddress listen = "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ";
            var done = new ManualResetEvent(false);
            var mdns = new Mdns
            {
                ServiceName = $"{Guid.NewGuid()}.local",
                Broadcast = false,
                Addresses = new List<MultiAddress> { listen }
            };
            mdns.PeerDiscovered += (s, e) =>
            {
                if (e.Address == listen)
                    done.Set();
            };
            await mdns.StartAsync();
            try
            {
                Assert.IsFalse(done.WaitOne(TimeSpan.FromSeconds(2)), "broadcast was sent");
            }
            finally
            {
                await mdns.StopAsync();
            }
        }

    }
}
