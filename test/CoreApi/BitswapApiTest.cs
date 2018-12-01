using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{

    [TestClass]
    public class BitswapApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;
        IpfsEngine ipfsOther = TestFixture.IpfsOther;

        [TestMethod]
        public async Task Wants()
        {
            var cts = new CancellationTokenSource();
            var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block"));
            Task wantTask = ipfs.Bitswap.GetAsync(block.Id, cts.Token);

            var endTime = DateTime.Now.AddSeconds(10);
            while (true)
            {
                if (DateTime.Now > endTime)
                    Assert.Fail("wanted block is missing");
                await Task.Delay(100);
                var w = await ipfs.Bitswap.WantsAsync();
                if (w.Contains(block.Id))
                    break;
            }

            cts.Cancel();
            var wants = await ipfs.Bitswap.WantsAsync();
            CollectionAssert.DoesNotContain(wants.ToArray(), block.Id);
            Assert.IsTrue(wantTask.IsCanceled);
        }

        [TestMethod]
        public async Task Unwant()
        {
            var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block 2"));
            Task wantTask = ipfs.Bitswap.GetAsync(block.Id);

            var endTime = DateTime.Now.AddSeconds(10);
            while (true)
            {
                if (DateTime.Now > endTime)
                    Assert.Fail("wanted block is missing");
                await Task.Delay(100);
                var w = await ipfs.Bitswap.WantsAsync();
                if (w.Contains(block.Id))
                    break;
            }

            await ipfs.Bitswap.UnwantAsync(block.Id);
            var wants = await ipfs.Bitswap.WantsAsync();
            CollectionAssert.DoesNotContain(wants.ToArray(), block.Id);
            Assert.IsTrue(wantTask.IsCanceled);
        }

        [TestMethod]
        public async Task OnConnect_Sends_WantList()
        {
            await ipfs.StartAsync();
            await ipfsOther.StartAsync();
            try
            {
                var local = await ipfs.LocalPeer;
                var remote = await ipfsOther.LocalPeer;
                var data = Guid.NewGuid().ToByteArray();
                var cid = new Cid { Hash = MultiHash.ComputeHash(data) };
                var _ = ipfs.Block.GetAsync(cid);
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());

                var endTime = DateTime.Now.AddSeconds(10);
                while (DateTime.Now < endTime)
                {
                    var wants = await ipfsOther.Bitswap.WantsAsync(local.Id);
                    if (wants.Contains(cid))
                        return;
                    await Task.Delay(200);
                }

                Assert.Fail("want list not sent");
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        public async Task GetsBlock_OnConnect()
        {
            await ipfs.StartAsync();
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var getTask = ipfs.Block.GetAsync(cid, cts.Token);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());
                var block = await getTask;

                Assert.IsFalse(getTask.IsCanceled, "task cancelled");
                Assert.IsFalse(getTask.IsFaulted, "task faulted");
                Assert.IsTrue(getTask.IsCompleted, "task not completed");
                Assert.AreEqual(cid, block.Id);
                CollectionAssert.AreEqual(data, block.DataBytes);
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        public async Task GetsBlock_OnRequest()
        {
            await ipfs.StartAsync();
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data);

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());

                var cts = new CancellationTokenSource(10000);
                var block = await ipfs.Block.GetAsync(cid, cts.Token);
                Assert.AreEqual(cid, block.Id);
                CollectionAssert.AreEqual(data, block.DataBytes);
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        public async Task GetsBlock_Cidv1()
        {
            await ipfs.StartAsync();
            await ipfsOther.StartAsync();
            try
            {
                var data = Guid.NewGuid().ToByteArray();
                var cid = await ipfsOther.Block.PutAsync(data, "raw", "sha2-512");

                var remote = await ipfsOther.LocalPeer;
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());

                var cts = new CancellationTokenSource(3000);
                var block = await ipfs.Block.GetAsync(cid, cts.Token);
                Assert.AreEqual(cid, block.Id);
                CollectionAssert.AreEqual(data, block.DataBytes);
            }
            finally
            {
                await ipfsOther.StopAsync();
                await ipfs.StopAsync();
            }
        }
        [TestMethod]
        public async Task GetBlock_Timeout()
        {
            var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block"));

            var cts = new CancellationTokenSource(300);
            ExceptionAssert.Throws<TaskCanceledException>(() =>
            {
                var _ = ipfs.Bitswap.GetAsync(block.Id, cts.Token).Result;
            });

            Assert.AreEqual(0, (await ipfs.Bitswap.WantsAsync()).Count());
        }

    }
}
