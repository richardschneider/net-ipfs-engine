using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.Multiplex
{
    [TestClass]
    public class SubstreamTest
    {
        [TestMethod]
        public void Seeking()
        {
            var stream = new Substream();
            Assert.IsFalse(stream.CanSeek);
            ExceptionAssert.Throws<NotSupportedException>(() => {
                stream.Seek(0, SeekOrigin.Begin);
            });
            ExceptionAssert.Throws<NotSupportedException>(() => {
                stream.Position = 0;
            });
            ExceptionAssert.Throws<NotSupportedException>(() => {
                var _ = stream.Position;
            });
        }

        [TestMethod]
        public void Timeout()
        {
            var stream = new Substream();
            Assert.IsFalse(stream.CanTimeout);
            ExceptionAssert.Throws<InvalidOperationException>(() => {
                stream.ReadTimeout = 0;
            });
            ExceptionAssert.Throws<InvalidOperationException>(() => {
                var _ = stream.ReadTimeout;
            });
            ExceptionAssert.Throws<InvalidOperationException>(() => {
                stream.WriteTimeout = 0;
            });
            ExceptionAssert.Throws<InvalidOperationException>(() => {
                var _ = stream.WriteTimeout;
            });
        }

        [TestMethod]
        public void Length()
        {
            var stream = new Substream();
            ExceptionAssert.Throws<NotSupportedException>(() => {
                stream.SetLength(0);
            });
            ExceptionAssert.Throws<NotSupportedException>(() => {
                var _ = stream.Length;
            });
        }

        [TestMethod]
        public async Task Reading()
        {
            var m1 = new byte[] { 1, 2, 3, 4 };
            var m2 = new byte[m1.Length];
            var stream = new Substream();
            stream.SetMessage(m1);
            Assert.IsTrue(stream.CanRead);

            m2[0] = (byte) stream.ReadByte();
            Assert.AreEqual(1, stream.Read(m2, 1, 1));
            Assert.AreEqual(2, await stream.ReadAsync(m2, 2, 2));
            CollectionAssert.AreEqual(m1, m2);

            Assert.AreEqual(-1, stream.ReadByte());
        }

        [TestMethod]
        public async Task Writing()
        {
            var ms = new MemoryStream();
            var muxer = new Muxer { Channel = ms };
            var stream = new Substream { Muxer = muxer };
            var m1 = new byte[1];
            stream.SetMessage(new byte[] { 10 });
            Assert.IsTrue(stream.CanRead);
            Assert.IsTrue(stream.CanWrite);

            Assert.AreEqual(1, await stream.ReadAsync(m1, 0, 1));
            await stream.WriteAsync(m1, 0, 1);
            stream.WriteByte(11);
            await stream.FlushAsync();

            ms.Position = 0;
            var header = await Header.ReadAsync(ms);
            var length = await Varint.ReadVarint32Async(ms);
            var payload = new byte[length];
            ms.Read(payload, 0, length);
            Assert.AreEqual(stream.Id, header.StreamId);
            Assert.AreEqual(2, payload.Length);
            CollectionAssert.AreEqual(new byte[] { 10, 11 }, payload);
        }
    }
}
