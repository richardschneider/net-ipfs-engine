using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer
{
    [TestClass]
    public class MessageTest
    {
        [TestMethod]
        public void Encoding()
        {
            var ms = new MemoryStream();
            Message.Write("a", ms);
            var buf = ms.ToArray();
            Assert.AreEqual(3, buf.Length);
            Assert.AreEqual(2, buf[0]);
            Assert.AreEqual((byte)'a', buf[1]);
            Assert.AreEqual((byte)'\n', buf[2]);
        }

        [TestMethod]
        public void RoundTrip()
        {
            var msg = "/foobar/0.42.0";
            var ms = new MemoryStream();
            Message.Write(msg, ms);
            ms.Position = 0;
            var result = Message.ReadString(ms);
            Assert.AreEqual(msg, result);
        }
    }
}
