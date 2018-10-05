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
                var _ = ipfs.Bitswap.GetAsync(cid);
                await ipfs.Swarm.ConnectAsync(remote.Addresses.First());

                var endTime = DateTime.Now.AddSeconds(3);
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
    }
}
