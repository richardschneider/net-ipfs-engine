using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer
{
    [TestClass]
    public class SwarmTest
    {
        MultiAddress mars = "/ip4/10.1.10.10/tcp/29087/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        MultiAddress venus = "/ip6/2604:a880:800:10::4a:5001/tcp/4001/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs6";

        [TestMethod]
        public async Task NewPeerAddress()
        {
            var swarm = new Swarm();
            await swarm.RegisterPeerAsync(mars);
            Assert.IsTrue(swarm.KnownPeerAddresses.Contains(mars));
        }

        [TestMethod]
        public void NewPeerAddress_BlackList()
        {
            var swarm = new Swarm();
            swarm.BlackList.Add(mars);

            Assert.IsFalse(swarm.RegisterPeerAsync(mars).Result);
            Assert.IsFalse(swarm.KnownPeerAddresses.Contains(mars));

            Assert.IsTrue(swarm.RegisterPeerAsync(venus).Result);
            Assert.IsTrue(swarm.KnownPeerAddresses.Contains(venus));
        }

        [TestMethod]
        public void NewPeerAddress_WhiteList()
        {
            var swarm = new Swarm();
            swarm.WhiteList.Add(venus);

            Assert.IsFalse(swarm.RegisterPeerAsync(mars).Result);
            Assert.IsFalse(swarm.KnownPeerAddresses.Contains(mars));

            Assert.IsTrue(swarm.RegisterPeerAsync(venus).Result);
            Assert.IsTrue(swarm.KnownPeerAddresses.Contains(venus));
        }

        [TestMethod]
        public async Task NewPeerAddress_InvalidAddress()
        {
            var swarm = new Swarm();
            await swarm.RegisterPeerAsync("/ip4/10.1.10.10/tcp/29087"); // missing ipfs protocol
            Assert.AreEqual(0, swarm.KnownPeerAddresses.Count());
        }

        [TestMethod]
        public async Task NewPeerAddress_Duplicate()
        {
            var swarm = new Swarm();
            await swarm.RegisterPeerAsync(mars);
            Assert.AreEqual(1, swarm.KnownPeerAddresses.Count());

            await swarm.RegisterPeerAsync(mars);
            Assert.AreEqual(1, swarm.KnownPeerAddresses.Count());
        }

        [TestMethod]
        public async Task KnownPeers()
        {
            var swarm = new Swarm();
            Assert.AreEqual(0, swarm.KnownPeers.Count());

            await swarm.RegisterPeerAsync("/ip4/10.1.10.10/tcp/29087/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3");
            Assert.AreEqual(1, swarm.KnownPeers.Count());

            await swarm.RegisterPeerAsync("/ip4/10.1.10.11/tcp/29087/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3");
            Assert.AreEqual(1, swarm.KnownPeers.Count());

            await swarm.RegisterPeerAsync(venus);
            Assert.AreEqual(2, swarm.KnownPeers.Count());
        }

        [TestMethod]
        public void Connecting_To_Blacklisted_Address()
        {
            var swarm = new Swarm();
            swarm.BlackList.Add(mars);
            ExceptionAssert.Throws<Exception>(() =>
            {
                swarm.ConnectAsync(mars).Wait();
            });
        }
    }
}
