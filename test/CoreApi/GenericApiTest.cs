using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    [TestClass]
    public class GenericApiTest
    {
        const string marsId = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";

        [TestMethod]
        public async Task Local_Info()
        {
            var ipfs = TestFixture.Ipfs;
            var peer = await ipfs.Generic.IdAsync();
            Assert.IsInstanceOfType(peer, typeof(Peer));
            Assert.IsNotNull(peer.Addresses);
            StringAssert.StartsWith(peer.AgentVersion, "net-ipfs/");
            Assert.IsNotNull(peer.Id);
            StringAssert.StartsWith(peer.ProtocolVersion, "ipfs/");
            Assert.IsNotNull(peer.PublicKey);

            Assert.IsTrue(peer.IsValid());
        }

        [TestMethod]
        [Ignore("NYI")]
        public async Task Mars_Info()
        {
            var ipfs = TestFixture.Ipfs;
            var peer = await ipfs.Generic.IdAsync(marsId);
            Assert.IsInstanceOfType(peer, typeof(Peer));
        }

        [TestMethod]
        public async Task Version_Info()
        {
            var ipfs = TestFixture.Ipfs;
            var versions = await ipfs.Generic.VersionAsync();
            Assert.IsNotNull(versions);
            Assert.IsTrue(versions.ContainsKey("Version"));
            Assert.IsTrue(versions.ContainsKey("Repo"));
        }

        [TestMethod]
        public async Task Shutdown()
        {
            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();
            await ipfs.Generic.ShutdownAsync();
        }

        [TestMethod]
        public async Task Resolve_Cid()
        {
            var ipfs = TestFixture.Ipfs;
            var actual = await ipfs.Generic.ResolveAsync("QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao");
            Assert.AreEqual("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao", actual);

            actual = await ipfs.Generic.ResolveAsync("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao");
            Assert.AreEqual("/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao", actual);
        }

        [TestMethod]
        public async Task Resolve_Cid_Path()
        {
            var ipfs = TestFixture.Ipfs;
            var temp = FileSystemApiTest.MakeTemp();
            try
            {
                var dir = await ipfs.FileSystem.AddDirectoryAsync(temp, true);
                var name = "/ipfs/" + dir.Id.Encode() + "/x/y/y.txt";
                Assert.AreEqual("/ipfs/QmTwEE2eSyzcvUctxP2negypGDtj7DQDKVy8s3Rvp6y6Pc", await ipfs.Generic.ResolveAsync(name));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [TestMethod]
        public void Resolve_Cid_Invalid()
        {
            var ipfs = TestFixture.Ipfs;
            ExceptionAssert.Throws<FormatException>(() =>
            {
                var _= ipfs.Generic.ResolveAsync("QmHash").Result;
            });
        }

        [TestMethod]
        public async Task Resolve_DnsLink()
        {
            var ipfs = TestFixture.Ipfs;
            var iopath = await ipfs.Generic.ResolveAsync("/ipns/ipfs.io");
            StringAssert.StartsWith(iopath, "/ipfs/");
       
            var media = await ipfs.Generic.ResolveAsync("/ipns/ipfs.io/media");
            Assert.AreEqual(iopath + "/media", media);
        }

        [TestMethod]
        [Ignore("Need a working bitswap")]
        public async Task Resolve_DnsLink_Recursive()
        {
            var ipfs = TestFixture.Ipfs;

            var media = await ipfs.Generic.ResolveAsync("/ipns/ipfs.io/media");
            var actual = await ipfs.Generic.ResolveAsync("/ipns/ipfs.io/media", recursive: true);
            Assert.AreNotEqual(media, actual);
        }
    }
}

