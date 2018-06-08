using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.BlockExchange
{
    [TestClass]
    public class BitswapTest
    {
        Peer self = new Peer
        {
            Id = "QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd"
        };

        [TestMethod]
        public void WantList()
        {
            var bitswap = new Bitswap();
            Assert.AreEqual(0, bitswap.PeerWants(self.Id).Count());

            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var task = bitswap.Want(cid, self.Id, cancel.Token);
            CollectionAssert.Contains(bitswap.PeerWants(self.Id).ToArray(), cid);

            bitswap.Unwant(cid);
            CollectionAssert.DoesNotContain(bitswap.PeerWants(self.Id).ToArray(), cid);
        }

        [TestMethod]
        public void Want_Cancel()
        {
            var bitswap = new Bitswap();
            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var task = bitswap.Want(cid, self.Id, cancel.Token);
            CollectionAssert.Contains(bitswap.PeerWants(self.Id).ToArray(), cid);

            cancel.Cancel();
            Assert.IsTrue(task.IsCanceled);
            CollectionAssert.DoesNotContain(bitswap.PeerWants(self.Id).ToArray(), cid);
        }

        [TestMethod]
        public void Want_Unwant()
        {
            var bitswap = new Bitswap();
            var cid = new DagNode(Encoding.UTF8.GetBytes("BitswapTest unknown block")).Id;
            var cancel = new CancellationTokenSource();
            var task = bitswap.Want(cid, self.Id, cancel.Token);
            CollectionAssert.Contains(bitswap.PeerWants(self.Id).ToArray(), cid);

            bitswap.Unwant(cid);
            Assert.IsTrue(task.IsCanceled);
            CollectionAssert.DoesNotContain(bitswap.PeerWants(self.Id).ToArray(), cid);
        }

        [TestMethod]
        public void Found()
        {
            var bitswap = new Bitswap();
            Assert.AreEqual(0, bitswap.PeerWants(self.Id).Count());

            var a = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block a"));
            var b = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block b"));
            var cancel = new CancellationTokenSource();
            var task = bitswap.Want(a.Id, self.Id, cancel.Token);
            Assert.IsFalse(task.IsCompleted);
            CollectionAssert.Contains(bitswap.PeerWants(self.Id).ToArray(), a.Id);

            bitswap.Found(b);
            Assert.IsFalse(task.IsCompleted);
            CollectionAssert.Contains(bitswap.PeerWants(self.Id).ToArray(), a.Id);

            bitswap.Found(a);
            Assert.IsTrue(task.IsCompleted);
            CollectionAssert.DoesNotContain(bitswap.PeerWants(self.Id).ToArray(), a.Id);
            CollectionAssert.AreEqual(a.DataBytes, task.Result.DataBytes);
        }

        [TestMethod]
        public void Found_Count()
        {
            var bitswap = new Bitswap();

            var a = new DagNode(Encoding.UTF8.GetBytes("BitswapTest found block a"));
            Assert.AreEqual(0, bitswap.Found(a));

            var cancel = new CancellationTokenSource();
            var task1 = bitswap.Want(a.Id, self.Id, cancel.Token);
            var task2 = bitswap.Want(a.Id, self.Id, cancel.Token);
            Assert.AreEqual(2, bitswap.Found(a));

            Assert.IsTrue(task1.IsCompleted);
            Assert.IsTrue(task2.IsCompleted);
        }

    }
}
