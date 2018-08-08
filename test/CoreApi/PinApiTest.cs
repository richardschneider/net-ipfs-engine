using Ipfs.CoreApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    [TestClass]
    public class PinApiTest
    {
        [TestMethod]
        public async Task Add_Remove()
        {
            var ipfs = TestFixture.Ipfs;
            var result = await ipfs.FileSystem.AddTextAsync("I am pinned");
            var id = result.Id;

            var pins = await ipfs.Pin.AddAsync(id);
            Assert.IsTrue(pins.Any(pin => pin == id));
            var all = await ipfs.Pin.ListAsync();
            Assert.IsTrue(all.Any(pin => pin == id));

            pins = await ipfs.Pin.RemoveAsync(id);
            Assert.IsTrue(pins.Any(pin => pin == id));
            all = await ipfs.Pin.ListAsync();
            Assert.IsFalse(all.Any(pin => pin == id));
        }

        [TestMethod]
        public async Task Remove_Unknown()
        {
            var ipfs = TestFixture.Ipfs;
            var dag = new DagNode(Encoding.UTF8.GetBytes("some unknown info for net-ipfs-engine-pin-test"));
            await ipfs.Pin.RemoveAsync(dag.Id, true);
        }

        [TestMethod]
        public async Task Inline_Cid()
        {
            var ipfs = TestFixture.Ipfs;
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(new byte[] { 1, 2, 3 }, "identity")
            };
            var pins = await ipfs.Pin.AddAsync(cid, recursive: false);
            CollectionAssert.Contains(pins.ToArray(), cid);
            var all = await ipfs.Pin.ListAsync();
            CollectionAssert.Contains(all.ToArray(), cid);

            var removals = await ipfs.Pin.RemoveAsync(cid, recursive: false);
            CollectionAssert.Contains(removals.ToArray(), cid);
            all = await ipfs.Pin.ListAsync();
            CollectionAssert.DoesNotContain(all.ToArray(), cid);
        }

        [TestMethod]
        public void Add_Unknown()
        {
            var ipfs = TestFixture.Ipfs;
            var dag = new DagNode(Encoding.UTF8.GetBytes("some unknown info for net-ipfs-engine-pin-test"));
            ExceptionAssert.Throws<Exception>(() =>
            {
                var cts = new CancellationTokenSource(250);
                var _ = ipfs.Pin.AddAsync(dag.Id, true, cts.Token).Result;
            });
        }

        [TestMethod]
        public async Task Add_Recursive()
        {
            var ipfs = TestFixture.Ipfs;
            var options = new AddFileOptions
            {
                ChunkSize = 3,
                Pin = false,
                RawLeaves = true,
                Wrap = true,
            };
            var node = await ipfs.FileSystem.AddTextAsync("hello world", options);
            var cids = await ipfs.Pin.AddAsync(node.Id, true);
            Assert.AreEqual(6, cids.Count());
        }
    }
}

