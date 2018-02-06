using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ipfs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace Peer2Peer
{
    [TestClass]
    public class MultiAddressExtensionsTest
    {
        [TestMethod]
        public void Cloning()
        {
            var a = new MultiAddress("/dns/libp2p.io/tcp/5001");
            var b = a.Clone();
            Assert.AreEqual(a, b);
            Assert.AreNotSame(a.Protocols, b.Protocols);
        }

        [TestMethod]
        public async Task Resolving()
        {
            var local = new MultiAddress("/ip4/127.0.0.1/tcp/5001");
            var r0 = await local.ResolveAsync();
            Assert.AreEqual(1, r0.Count);
            Assert.AreEqual(local, r0[0]);

            var dns = await new MultiAddress("/dns/libp2p.io/tcp/5001").ResolveAsync();
            Assert.AreNotEqual(0, dns.Count);
            var dns4 = await new MultiAddress("/dns4/libp2p.io/tcp/5001").ResolveAsync();
            Assert.AreNotEqual(0, dns4.Count);
            var dns6 = await new MultiAddress("/dns6/libp2p.io/tcp/5001").ResolveAsync();
            Assert.AreNotEqual(0, dns6.Count);
            Assert.AreEqual(dns.Count, dns4.Count + dns6.Count);
        }

        [TestMethod]
        public void Resolving_Unknown()
        {
            ExceptionAssert.Throws<SocketException>(() =>
            {
                var _ = new MultiAddress("/dns/does.not.exist/tcp/5001")
                    .ResolveAsync()
                    .Result;
            });
        }

    }
}
