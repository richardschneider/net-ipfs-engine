using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Routing
{
    
    [TestClass]
    public class DistributedQueryTest
    {
        [TestMethod]
        public void Cancelling()
        {
            var dquery = new DistributedQuery<Peer>();
            var cts = new CancellationTokenSource();
            cts.Cancel();
            ExceptionAssert.Throws<TaskCanceledException>(() =>
            {
                dquery.Run(cts.Token).Wait();
            });
        }

        [TestMethod]
        public void UniqueId()
        {
            var q1 = new DistributedQuery<Peer>();
            var q2 = new DistributedQuery<Peer>();
            Assert.AreNotEqual(q1.Id, q2.Id);
        }
    }
}
