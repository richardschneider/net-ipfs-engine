using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    [TestClass]
    public class PeerConnectionTest
    {
        [TestMethod]
        public void Disposing()
        {
            var stream = new MemoryStream();
            var connection = new PeerConnection { Stream = stream };
            Assert.IsNotNull(connection.Stream);

            connection.Dispose();
            Assert.IsNull(connection.Stream);

            // Can be disposed multiple times.
            connection.Dispose();
        }

        [TestMethod]
        public void Stats()
        {
            var stream = new MemoryStream();
            var connection = new PeerConnection { Stream = stream };
            Assert.AreEqual(0, connection.BytesRead);
            Assert.AreEqual(0, connection.BytesWritten);

            var buffer = new byte[] { 1, 2, 3 };
            connection.Stream.Write(buffer, 0, 3);
            Assert.AreEqual(0, connection.BytesRead);
            Assert.AreEqual(3, connection.BytesWritten);

            stream.Position = 0;
            connection.Stream.ReadByte();
            connection.Stream.ReadByte();
            Assert.AreEqual(2, connection.BytesRead);
            Assert.AreEqual(3, connection.BytesWritten);
        }
    }
}
