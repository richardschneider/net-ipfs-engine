using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;
using System;
using System.IO;

namespace ProtobufTesting
{
    [ProtoContract]
    class M1
    {
        [ProtoMember(1, IsRequired = false)]
        public byte[] Data;
    }

    [ProtoContract]
    class M2
    {
        [ProtoMember(1, IsRequired = false)]
        public ArraySegment<byte>? Data;
    }

    [TestClass]
    [Ignore("https://github.com/mgravell/protobuf-net/issues/368")]
    public class ProtobufTest
    {
        [TestMethod]
        public void NullData()
        {
            var m1 = new M1();
            var ms1 = new MemoryStream();
            Serializer.Serialize<M1>(ms1, m1);
            var bytes1 = ms1.ToArray();

            var m2 = new M2();
            var ms2 = new MemoryStream();
            Serializer.Serialize<M2>(ms2, m2);
            var bytes2 = ms2.ToArray();

            CollectionAssert.AreEqual(bytes1, bytes2);
        }

        [TestMethod]
        public void SomeData()
        {
            var data = new byte[] { 10, 11, 12 };
            var m1 = new M1 { Data = data };
            var ms1 = new MemoryStream();
            Serializer.Serialize<M1>(ms1, m1);
            var bytes1 = ms1.ToArray();

            var m2 = new M2 { Data = new ArraySegment<byte>(data) };
            var ms2 = new MemoryStream();
            Serializer.Serialize<M2>(ms2, m2);
            var bytes2 = ms2.ToArray();

            CollectionAssert.AreEqual(bytes1, bytes2);
        }
    }
}
