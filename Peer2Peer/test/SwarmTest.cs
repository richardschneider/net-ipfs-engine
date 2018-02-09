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
        MultiAddress venus = "/ip4/104.236.76.40/tcp/4001/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64";
        MultiAddress earth = "/ip4/178.62.158.247/tcp/4001/ipfs/QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd";
        Peer self = new Peer
        {
            Id = "QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd"
        };

        [TestMethod]
        public async Task NewPeerAddress()
        {
            var swarm = new Swarm { LocalPeer = self };
            await swarm.RegisterPeerAsync(mars);
            Assert.IsTrue(swarm.KnownPeerAddresses.Contains(mars));
        }

        [TestMethod]
        public void NewPeerAddress_Self()
        {
            var swarm = new Swarm { LocalPeer = self };
            Assert.IsFalse(swarm.RegisterPeerAsync(earth).Result);
        }

        [TestMethod]
        public void NewPeerAddress_BlackList()
        {
            var swarm = new Swarm { LocalPeer = self };
            swarm.BlackList.Add(mars);

            Assert.IsFalse(swarm.RegisterPeerAsync(mars).Result);
            Assert.IsFalse(swarm.KnownPeerAddresses.Contains(mars));

            Assert.IsTrue(swarm.RegisterPeerAsync(venus).Result);
            Assert.IsTrue(swarm.KnownPeerAddresses.Contains(venus));
        }

        [TestMethod]
        public void NewPeerAddress_WhiteList()
        {
            var swarm = new Swarm { LocalPeer = self };
            swarm.WhiteList.Add(venus);

            Assert.IsFalse(swarm.RegisterPeerAsync(mars).Result);
            Assert.IsFalse(swarm.KnownPeerAddresses.Contains(mars));

            Assert.IsTrue(swarm.RegisterPeerAsync(venus).Result);
            Assert.IsTrue(swarm.KnownPeerAddresses.Contains(venus));
        }

        [TestMethod]
        public async Task NewPeerAddress_InvalidAddress()
        {
            var swarm = new Swarm { LocalPeer = self };
            await swarm.RegisterPeerAsync("/ip4/10.1.10.10/tcp/29087"); // missing ipfs protocol
            Assert.AreEqual(0, swarm.KnownPeerAddresses.Count());
        }

        [TestMethod]
        public async Task NewPeerAddress_Duplicate()
        {
            var swarm = new Swarm { LocalPeer = self };
            await swarm.RegisterPeerAsync(mars);
            Assert.AreEqual(1, swarm.KnownPeerAddresses.Count());

            await swarm.RegisterPeerAsync(mars);
            Assert.AreEqual(1, swarm.KnownPeerAddresses.Count());
        }

        [TestMethod]
        public async Task KnownPeers()
        {
            var swarm = new Swarm { LocalPeer = self };
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
            var swarm = new Swarm { LocalPeer = self };
            swarm.BlackList.Add(mars);
            ExceptionAssert.Throws<Exception>(() =>
            {
                swarm.ConnectAsync(mars).Wait();
            });
        }

        [TestMethod]
        public void Connecting_To_Self()
        {
            var swarm = new Swarm { LocalPeer = self };
            ExceptionAssert.Throws<Exception>(() =>
            {
                swarm.ConnectAsync(earth).Wait();
            });
        }
    }
}
