using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    [TestClass]
    public class SwarmTest
    {
        MultiAddress mars = "/ip4/10.1.10.10/tcp/29087/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        MultiAddress venus = "/ip4/104.236.76.40/tcp/4001/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64";
        MultiAddress earth = "/ip4/178.62.158.247/tcp/4001/ipfs/QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd";
        Peer self = new Peer
        {
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
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
            var selfAddress = "/ip4/178.62.158.247/tcp/4001/ipfs/" + self.Id;
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = swarm.RegisterPeerAsync(selfAddress).Result;
            });

            selfAddress = "/ip4/178.62.158.247/tcp/4001/p2p/" + self.Id;
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = swarm.RegisterPeerAsync(selfAddress).Result;
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

            await swarm.RegisterPeerAsync("/ip4/10.1.10.11/tcp/29087/p2p/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3");
            Assert.AreEqual(1, swarm.KnownPeers.Count());
            Assert.AreEqual(2, swarm.KnownPeerAddresses.Count());

            await swarm.RegisterPeerAsync(venus);
            Assert.AreEqual(2, swarm.KnownPeers.Count());
            Assert.AreEqual(3, swarm.KnownPeerAddresses.Count());
        }

        [TestMethod]
        public async Task Connect_Disconnect()
        {
            var peerB = new Peer
            {
                Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
                PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQDlTSgVLprWaXfmxDr92DJE1FP0wOexhulPqXSTsNh5ot6j+UiuMgwb0shSPKzLx9AuTolCGhnwpTBYHVhFoBErAgMBAAE="
            };
            var swarmB = new Swarm { LocalPeer = peerB };
            var peerBAddress = await swarmB.StartListeningAsync("/ip4/127.0.0.1/tcp/0");
            Assert.IsTrue(peerB.Addresses.Count() > 0);

            var swarm = new Swarm { LocalPeer = self };
            await swarm.StartAsync();
            try
            {
                var remotePeer = await swarm.ConnectAsync(peerBAddress);
                Assert.IsNotNull(remotePeer.ConnectedAddress);
                Assert.AreEqual(peerB.PublicKey, remotePeer.PublicKey);
                Assert.IsTrue(remotePeer.IsValid());
                Assert.IsTrue(swarm.KnownPeers.Contains(peerB));
                Assert.IsTrue(swarmB.KnownPeers.Contains(self));

                await swarm.DisconnectAsync(peerBAddress);
                Assert.IsNull(remotePeer.ConnectedAddress);
                Assert.IsTrue(swarm.KnownPeers.Contains(peerB));
                Assert.IsTrue(swarmB.KnownPeers.Contains(self));
            }
            finally
            {
                await swarm.StopAsync();
                await swarmB.StopAsync();
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
        public void Connect_Not_Peer()
        {
            var remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            var remoteAddress = $"/dns/npmjs.com/tcp/80/ipfs/{remoteId}";
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

        [TestMethod]
        public async Task Listening()
        {
            var peerA = new Peer { Id = "QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd" };
            MultiAddress addr = "/ip4/127.0.0.1/tcp/4009";
            var swarmA = new Swarm { LocalPeer = peerA };
            var peerB = new Peer { Id = "QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64" };
            var swarmB = new Swarm { LocalPeer = peerB };
            try
            {
                var another = await swarmA.StartListeningAsync(addr);
                Assert.AreEqual(another.ToString(), $"{addr}/ipfs/{peerA.Id}");
                Assert.IsTrue(peerA.Addresses.Contains(another));

                await swarmB.ConnectAsync(another);
                Assert.IsTrue(swarmB.KnownPeers.Contains(peerA));
                // TODO: Assert.IsTrue(swarmA.KnownPeers.Contains(peerB));

                await swarmA.StopListeningAsync(addr);
                Assert.AreEqual(0, peerA.Addresses.Count());
            }
            finally
            {
                await swarmA.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [TestMethod]
        public async Task Listening_Event()
        {
            var peer = new Peer { Id = "QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd" };
            MultiAddress addr = "/ip4/127.0.0.1/tcp/4009";
            var swarm = new Swarm { LocalPeer = peer };
            Peer listeningPeer = null;
            swarm.ListenerEstablished += (s, e) =>
            {
                listeningPeer = e;
            };
            try
            {
                await swarm.StartListeningAsync(addr);
                Assert.AreEqual(peer, listeningPeer);
                Assert.AreNotEqual(0, peer.Addresses.Count());
            }
            finally
            {
                await swarm.StopAsync();
            }
        }

        [TestMethod]
        public async Task Listening_AnyPort()
        {
            var peerA = new Peer { Id = "QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd" };
            MultiAddress addr = "/ip4/127.0.0.1/tcp/0";
            var swarmA = new Swarm { LocalPeer = peerA };
            var peerB = new Peer { Id = "QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64" };
            var swarmB = new Swarm { LocalPeer = peerB };
            try
            {
                var another = await swarmA.StartListeningAsync(addr);
                Assert.IsTrue(peerA.Addresses.Contains(another));

                await swarmB.ConnectAsync(another);
                Assert.IsTrue(swarmB.KnownPeers.Contains(peerA));
                // TODO: Assert.IsTrue(swarmA.KnownPeers.Contains(peerB));

                await swarmA.StopListeningAsync(addr);
                Assert.IsFalse(peerA.Addresses.Contains(another));
            }
            finally
            {
                await swarmA.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [TestMethod]
        public async Task Listening_IPv4Any()
        {
            var peerA = new Peer { Id = "QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd" };
            MultiAddress addr = "/ip4/0.0.0.0/tcp/0";
            var swarmA = new Swarm { LocalPeer = peerA };
            var peerB = new Peer { Id = "QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64" };
            var swarmB = new Swarm { LocalPeer = peerB };
            try
            {
                var another = await swarmA.StartListeningAsync(addr);
                Assert.IsFalse(peerA.Addresses.Contains(addr));
                Assert.IsTrue(peerA.Addresses.Contains(another));

                await swarmB.ConnectAsync(another);
                Assert.IsTrue(swarmB.KnownPeers.Contains(peerA));
                // TODO: Assert.IsTrue(swarmA.KnownPeers.Contains(peerB));

                await swarmA.StopListeningAsync(addr);
                Assert.AreEqual(0, peerA.Addresses.Count());
            }
            finally
            {
                await swarmA.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [TestMethod]
        [TestCategory("IPv6")]
        public async Task Listening_IPv6Any()
        {
            var peerA = new Peer { Id = "QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd" };
            MultiAddress addr = "/ip6/::/tcp/0";
            var swarmA = new Swarm { LocalPeer = peerA };
            var peerB = new Peer { Id = "QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64" };
            var swarmB = new Swarm { LocalPeer = peerB };
            try
            {
                var another = await swarmA.StartListeningAsync(addr);
                Assert.IsFalse(peerA.Addresses.Contains(addr));
                Assert.IsTrue(peerA.Addresses.Contains(another));

                await swarmB.ConnectAsync(another);
                Assert.IsTrue(swarmB.KnownPeers.Contains(peerA));
                // TODO: Assert.IsTrue(swarmA.KnownPeers.Contains(peerB));

                await swarmA.StopListeningAsync(addr);
                Assert.AreEqual(0, peerA.Addresses.Count());
            }
            finally
            {
                await swarmA.StopAsync();
                await swarmB.StopAsync();
            }
        }

        [TestMethod]
        public void Listening_MissingTransport()
        {
            var peer = new Peer { Id = "QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd" };
            var swarm = new Swarm { LocalPeer = peer };
            ExceptionAssert.Throws<ArgumentException>(() =>
            {
                var _ = swarm.StartListeningAsync("/ip4/127.0.0.1").Result;
            });
            Assert.AreEqual(0, peer.Addresses.Count());
        }

        [TestMethod]
        [Ignore("TODO: Move to interop tests")]
        public async Task JsIPFS_Connect()
        {
            var remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            var remoteAddress = $"/ip4/127.0.0.1/tcp/4002/ipfs/{remoteId}";

            var swarm = new Swarm { LocalPeer = self };
            await swarm.StartAsync();
            try
            {
                var remotePeer = await swarm.ConnectAsync(remoteAddress);
                Assert.IsNotNull(remotePeer.ConnectedAddress);
                Assert.IsTrue(swarm.KnownPeers.Contains(remotePeer));
                Assert.IsFalse(swarm.KnownPeers.Contains(self));
                Assert.IsTrue(remotePeer.IsValid());
                await Task.Delay(2000);

                await swarm.DisconnectAsync(remoteAddress);
                Assert.IsNull(remotePeer.ConnectedAddress);
                Assert.IsTrue(swarm.KnownPeers.Contains(remotePeer));
                Assert.IsFalse(swarm.KnownPeers.Contains(self));
            }
            finally
            {
                await swarm.StopAsync();
            }
        }

        [TestMethod]
        [Ignore("TODO: Move to interop tests")]
        public async Task GoIPFS_Connect()
        {
            var remoteId = "QmdoxrwszT6b9srLXHYBPFVRXmZSFAosWLXoQS9TEEAaix";
            var remoteAddress = $"/ip4/127.0.0.1/tcp/4001/ipfs/{remoteId}";

            var swarm = new Swarm { LocalPeer = self };
            await swarm.StartAsync();
            try
            {
                var remotePeer = await swarm.ConnectAsync(remoteAddress);
                Assert.IsNotNull(remotePeer.ConnectedAddress);
                Assert.IsTrue(swarm.KnownPeers.Contains(remotePeer));
                Assert.IsFalse(swarm.KnownPeers.Contains(self));
                Assert.IsTrue(remotePeer.IsValid());
                await Task.Delay(2000);

                await swarm.DisconnectAsync(remoteAddress);
                Assert.IsNull(remotePeer.ConnectedAddress);
                Assert.IsTrue(swarm.KnownPeers.Contains(remotePeer));
                Assert.IsFalse(swarm.KnownPeers.Contains(self));
            }
            finally
            {
                await swarm.StopAsync();
            }
        }
    }
}

