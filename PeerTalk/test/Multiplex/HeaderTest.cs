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
    public class HeaderTest
    {
        [TestMethod]
        public void StreamIds()
        {
            Roundtrip(0, PacketType.NewStream);
            Roundtrip(1, PacketType.NewStream);
            Roundtrip(0x1234, PacketType.NewStream);
            Roundtrip(0x12345678, PacketType.NewStream);
            Roundtrip(Header.MinStreamId, PacketType.NewStream);
            Roundtrip(Header.MaxStreamId, PacketType.NewStream);
        }

        void Roundtrip(long id, PacketType type)
        {
            var header1 = new Header { StreamId = id, PacketType = type };
            var ms = new MemoryStream();
            header1.WriteAsync(ms).Wait();
            ms.Position = 0;
            var header2 = Header.ReadAsync(ms).Result;
            Assert.AreEqual(header1.StreamId, header2.StreamId);
            Assert.AreEqual(header1.PacketType, header2.PacketType);
        }
    }
}
