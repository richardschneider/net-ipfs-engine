using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using PeterO.Cbor;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine.LinkedData
{
    [TestClass]
    public class RawFormatTest
    {
        ILinkedDataFormat formatter = new RawFormat();

        [TestMethod]
        public void Empty()
        {
            var data = new byte[0];

            var cbor = formatter.Deserialise(data);
            CollectionAssert.AreEqual(data, cbor["data"].GetByteString());

            var data1 = formatter.Serialize(cbor);
            CollectionAssert.AreEqual(data, data1);
        }

        [TestMethod]
        public void Data()
        {
            var data = Encoding.UTF8.GetBytes("abc");

            var cbor = formatter.Deserialise(data);
            CollectionAssert.AreEqual(data, cbor["data"].GetByteString());

            var data1 = formatter.Serialize(cbor);
            CollectionAssert.AreEqual(data, data1);
        }

    }
}
