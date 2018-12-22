using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PeerTalk.Protocols;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    [TestClass]
    public class ConnectionManagerTest
    {
        MultiHash aId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
        MultiHash bId = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h";

        [TestMethod]
        public void IsConnected()
        {
            var manager = new ConnectionManager();
            var peer = new Peer { Id = aId };
            var connection = new PeerConnection { RemotePeer = peer, Stream = Stream.Null };

            Assert.IsFalse(manager.IsConnected(peer));
            manager.Add(connection);
            Assert.IsTrue(manager.IsConnected(peer));
        }

        [TestMethod]
        public void IsConnected_NotActive()
        {
            var manager = new ConnectionManager();
            var peer = new Peer { Id = aId };
            var connection = new PeerConnection { RemotePeer = peer, Stream = Stream.Null };

            Assert.IsFalse(manager.IsConnected(peer));

            manager.Add(connection);
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());

            connection.Stream = null;
            Assert.IsFalse(manager.IsConnected(peer));
            Assert.AreEqual(0, manager.Connections.Count());
        }

        [TestMethod]
        public void Add_Duplicate()
        {
            var manager = new ConnectionManager();
            var peer = new Peer { Id = aId };
            var a = new PeerConnection { RemotePeer = peer, Stream = Stream.Null };
            var b = new PeerConnection { RemotePeer = peer, Stream = Stream.Null };

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);

            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(2, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.IsNotNull(b.Stream);

            manager.Clear();
            Assert.AreEqual(0, manager.Connections.Count());
            Assert.IsNull(a.Stream);
            Assert.IsNull(b.Stream);
        }

        [TestMethod]
        public void Add_Duplicate_SameConnection()
        {
            var manager = new ConnectionManager();
            var peer = new Peer { Id = aId };
            var a = new PeerConnection { RemotePeer = peer, Stream = Stream.Null };

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
        }

        [TestMethod]
        public void Add_Duplicate_PeerConnectedAddress()
        {
            var address = "/ip6/::1/tcp/4007";

            var manager = new ConnectionManager();
            var peer = new Peer { Id = aId, ConnectedAddress = address };
            var a = new PeerConnection { RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null };
            var b = new PeerConnection { RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null };

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.AreEqual(address, peer.ConnectedAddress);

            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(2, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.IsNotNull(b.Stream);
            Assert.AreEqual(address, peer.ConnectedAddress);
        }

        [TestMethod]
        public void Remove_Duplicate_PeerConnectedAddress()
        {
            var address = "/ip6/::1/tcp/4007";

            var manager = new ConnectionManager();
            var peer = new Peer { Id = aId, ConnectedAddress = address };
            var a = new PeerConnection { RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null };
            var b = new PeerConnection { RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null };

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.AreEqual(address, peer.ConnectedAddress);

            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(2, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.IsNotNull(b.Stream);
            Assert.AreEqual(address, peer.ConnectedAddress);

            Assert.IsTrue(manager.Remove(b));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.IsNull(b.Stream);
            Assert.AreEqual(address, peer.ConnectedAddress);
        }

        [TestMethod]
        public void Add_Duplicate_ExistingIsDead()
        {
            var address = "/ip6/::1/tcp/4007";

            var manager = new ConnectionManager();
            var peer = new Peer { Id = aId, ConnectedAddress = address };
            var a = new PeerConnection { RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null };
            var b = new PeerConnection { RemotePeer = peer, RemoteAddress = address, Stream = Stream.Null };

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.AreEqual(address, peer.ConnectedAddress);

            a.Stream = null;
            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNull(a.Stream);
            Assert.IsNotNull(b.Stream);
            Assert.AreEqual(address, peer.ConnectedAddress);
        }

        [TestMethod]
        public void Add_NotActive()
        {
            var manager = new ConnectionManager();
            var peer = new Peer { Id = aId };
            var a = new PeerConnection { RemotePeer = peer, Stream = Stream.Null };
            var b = new PeerConnection { RemotePeer = peer, Stream = Stream.Null };

            Assert.AreSame(a, manager.Add(a));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            a.Stream = null;

            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNull(a.Stream);
            Assert.IsNotNull(b.Stream);

            Assert.AreSame(b, manager.Connections.First());
        }

        [TestMethod]
        public void Remove_Connection()
        {
            var manager = new ConnectionManager();
            var peer = new Peer { Id = aId };
            var a = new PeerConnection { RemotePeer = peer, Stream = Stream.Null };

            manager.Add(a);
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);

            Assert.IsTrue(manager.Remove(a));
            Assert.IsFalse(manager.IsConnected(peer));
            Assert.AreEqual(0, manager.Connections.Count());
            Assert.IsNull(a.Stream);
        }


        [TestMethod]
        public void Remove_PeerId()
        {
            var manager = new ConnectionManager();
            var peer = new Peer { Id = aId };
            var a = new PeerConnection { RemotePeer = peer, Stream = Stream.Null };

            manager.Add(a);
            Assert.IsTrue(manager.IsConnected(peer));
            Assert.AreEqual(1, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);

            Assert.IsTrue(manager.Remove(peer.Id));
            Assert.IsFalse(manager.IsConnected(peer));
            Assert.AreEqual(0, manager.Connections.Count());
            Assert.IsNull(a.Stream);
        }

        [TestMethod]
        public void Remove_DoesNotExist()
        {
            var manager = new ConnectionManager();
            var peer = new Peer { Id = aId };
            var a = new PeerConnection { RemotePeer = peer, Stream = Stream.Null };

            Assert.IsFalse(manager.Remove(a));
            Assert.IsFalse(manager.IsConnected(peer));
            Assert.AreEqual(0, manager.Connections.Count());
            Assert.IsNull(a.Stream);
        }

        [TestMethod]
        public void Clear()
        {
            var manager = new ConnectionManager();
            var peerA = new Peer { Id = aId };
            var peerB = new Peer { Id = bId };
            var a = new PeerConnection { RemotePeer = peerA, Stream = Stream.Null };
            var b = new PeerConnection { RemotePeer = peerB, Stream = Stream.Null };

            Assert.AreSame(a, manager.Add(a));
            Assert.AreSame(b, manager.Add(b));
            Assert.IsTrue(manager.IsConnected(peerA));
            Assert.IsTrue(manager.IsConnected(peerB));
            Assert.AreEqual(2, manager.Connections.Count());
            Assert.IsNotNull(a.Stream);
            Assert.IsNotNull(b.Stream);

            manager.Clear();
            Assert.IsFalse(manager.IsConnected(peerA));
            Assert.IsFalse(manager.IsConnected(peerB));
            Assert.AreEqual(0, manager.Connections.Count());
            Assert.IsNull(a.Stream);
            Assert.IsNull(b.Stream);
        }
    }
}
