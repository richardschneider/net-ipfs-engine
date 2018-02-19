using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer
{
    [TestClass]
    public class PeerConnectionTest
    {
        [TestMethod]
        public void Disposing()
        {
            var stream = new MemoryStream();
            var connection = new PeerConnection { Stream = stream };
            Assert.AreEqual(stream, connection.Stream);

            connection.Dispose();
            Assert.IsNull(connection.Stream);

            // Can be disposed multiple times.
            connection.Dispose();
        }
    }
}
