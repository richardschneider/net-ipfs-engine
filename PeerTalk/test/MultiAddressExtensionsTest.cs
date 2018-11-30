using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ipfs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace PeerTalk
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
        public void WithPeerId()
        {
            var id = "QmQusTXc1Z9C1mzxsqC9ZTFXCgSkpBRGgW4Jk2QYHxKE22";
            var id3 = "QmQusTXc1Z9C1mzxsqC9ZTFXCgSkpBRGgW4Jk2QYHxKE33";

            var ma1 = new MultiAddress("/ip4/127.0.0.1/tcp/4001");
            Assert.AreEqual($"{ma1}/p2p/{id}", ma1.WithPeerId(id));

            ma1 = new MultiAddress($"/ip4/127.0.0.1/tcp/4001/ipfs/{id}");
            Assert.AreSame(ma1, ma1.WithPeerId(id));

            ma1 = new MultiAddress($"/ip4/127.0.0.1/tcp/4001/p2p/{id}");
            Assert.AreSame(ma1, ma1.WithPeerId(id));

            ExceptionAssert.Throws<Exception>(() =>
            {
                ma1 = new MultiAddress($"/ip4/127.0.0.1/tcp/4001/ipfs/{id3}");
                Assert.AreSame(ma1, ma1.WithPeerId(id));
            });
        }

        [TestMethod]
        public void WithoutPeerId()
        {
            var id = "QmQusTXc1Z9C1mzxsqC9ZTFXCgSkpBRGgW4Jk2QYHxKE22";

            var ma1 = new MultiAddress("/ip4/127.0.0.1/tcp/4001");
            Assert.AreSame(ma1, ma1.WithoutPeerId());

            ma1 = new MultiAddress($"/ip4/127.0.0.1/tcp/4001/ipfs/{id}");
            Assert.AreEqual("/ip4/127.0.0.1/tcp/4001", ma1.WithoutPeerId());

            ma1 = new MultiAddress($"/ip4/127.0.0.1/tcp/4001/p2p/{id}");
            Assert.AreEqual("/ip4/127.0.0.1/tcp/4001", ma1.WithoutPeerId());
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
            var dns6 = await new MultiAddress("/dns6/libp2p.io/tcp/5001").ResolveAsync();
            Assert.AreEqual(dns.Count, dns4.Count + dns6.Count);
        }

        [TestMethod]
        public async Task Resolving_HTTP()
        {
            var r = await new MultiAddress("/ip4/127.0.0.1/http").ResolveAsync();
            Assert.AreEqual("/ip4/127.0.0.1/http/tcp/80", r.First());

            r = await new MultiAddress("/ip4/127.0.0.1/http/tcp/8080").ResolveAsync();
            Assert.AreEqual("/ip4/127.0.0.1/http/tcp/8080", r.First());
        }

        [TestMethod]
        public async Task Resolving_HTTPS()
        {
            var r = await new MultiAddress("/ip4/127.0.0.1/https").ResolveAsync();
            Assert.AreEqual("/ip4/127.0.0.1/https/tcp/443", r.First());

            r = await new MultiAddress("/ip4/127.0.0.1/https/tcp/4433").ResolveAsync();
            Assert.AreEqual("/ip4/127.0.0.1/https/tcp/4433", r.First());
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
