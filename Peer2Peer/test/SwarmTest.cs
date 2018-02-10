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
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = swarm.RegisterPeerAsync(earth).Result;
            });
        }

        [TestMethod]
        public void NewPeerAddress_BlackList()
        {
            var swarm = new Swarm { LocalPeer = self };
            swarm.BlackList.Add(mars);

            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = swarm.RegisterPeerAsync(mars).Result;
            });
            Assert.IsFalse(swarm.KnownPeerAddresses.Contains(mars));

            Assert.IsNotNull(swarm.RegisterPeerAsync(venus).Result);
            Assert.IsTrue(swarm.KnownPeerAddresses.Contains(venus));
        }

        [TestMethod]
        public void NewPeerAddress_WhiteList()
        {
            var swarm = new Swarm { LocalPeer = self };
            swarm.WhiteList.Add(venus);

            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = swarm.RegisterPeerAsync(mars).Result;
            });
            Assert.IsFalse(swarm.KnownPeerAddresses.Contains(mars));

            Assert.IsNotNull(swarm.RegisterPeerAsync(venus).Result);
            Assert.IsTrue(swarm.KnownPeerAddresses.Contains(venus));
        }

        [TestMethod]
        public void NewPeerAddress_InvalidAddress()
        {
            var swarm = new Swarm { LocalPeer = self };
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = swarm.RegisterPeerAsync("/ip4/10.1.10.10/tcp/29087").Result;
            });
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
            Assert.AreEqual(0, swarm.KnownPeerAddresses.Count());

            await swarm.RegisterPeerAsync("/ip4/10.1.10.10/tcp/29087/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3");
            Assert.AreEqual(1, swarm.KnownPeers.Count());
            Assert.AreEqual(1, swarm.KnownPeerAddresses.Count());

            await swarm.RegisterPeerAsync("/ip4/10.1.10.11/tcp/29087/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3");
            Assert.AreEqual(1, swarm.KnownPeers.Count());
            Assert.AreEqual(2, swarm.KnownPeerAddresses.Count());

            await swarm.RegisterPeerAsync(venus);
            Assert.AreEqual(2, swarm.KnownPeers.Count());
            Assert.AreEqual(3, swarm.KnownPeerAddresses.Count());
        }

        [TestMethod]
        public async Task Connect_Disconnect()
        {
            var remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            var remoteAddress = $"/ip4/127.0.0.1/tcp/4002/ipfs/{remoteId}";
            var swarm = new Swarm { LocalPeer = self };
            await swarm.StartAsync();
            try
            {
                var remotePeer = await swarm.ConnectAsync(remoteAddress);
                Assert.IsNotNull(remotePeer.ConnectedAddress);

                await swarm.DisconnectAsync(remoteAddress);
                Assert.IsNull(remotePeer.ConnectedAddress);
            }
            finally
            {
                await swarm.StopAsync();
            }
        }

        [TestMethod]
        public void Connect_No_Transport()
        {
            var remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            var remoteAddress = $"/ip4/127.0.0.1/ipfs/{remoteId}";
            var swarm = new Swarm { LocalPeer = self };
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = swarm.ConnectAsync(remoteAddress).Result;
            });
        }

        [TestMethod]
        public void Connect_Refused()
        {
            var remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            var remoteAddress = $"/ip4/127.0.0.1/tcp/4040/ipfs/{remoteId}";
            var swarm = new Swarm { LocalPeer = self };
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = swarm.ConnectAsync(remoteAddress).Result;
            });
        }

        [TestMethod]
        public async Task Connect_Cancelled()
        {
            var cs = new CancellationTokenSource();
            cs.Cancel();
            var remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            var remoteAddress = $"/ip4/127.0.0.1/tcp/4002/ipfs/{remoteId}";
            var swarm = new Swarm { LocalPeer = self };
            var remotePeer = await swarm.ConnectAsync(remoteAddress, cs.Token);
            Assert.IsNull(remotePeer);
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
