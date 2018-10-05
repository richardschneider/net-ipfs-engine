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
    public class PeerConnectionTest
    {
        [TestMethod]
        public void Disposing()
        {
            var closeCount = 0;
            var stream = new MemoryStream();
            var connection = new PeerConnection { Stream = stream };
            connection.Closed += (s, e) =>
            {
                ++closeCount;
            };
            Assert.IsNotNull(connection.Stream);

            connection.Dispose();
            Assert.IsNull(connection.Stream);

            // Can be disposed multiple times.
            connection.Dispose();

            Assert.AreEqual(1, closeCount);
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

        [TestMethod]
        public void Protocols()
        {
            var connection = new PeerConnection();
            Assert.AreEqual(0, connection.Protocols.Count);

            connection.AddProtocol(new Identify1());
            Assert.AreEqual(1, connection.Protocols.Count);

            connection.AddProtocols(new IPeerProtocol[] { new Mplex67(), new Plaintext1() });
            Assert.AreEqual(3, connection.Protocols.Count);
        }
    }
}
