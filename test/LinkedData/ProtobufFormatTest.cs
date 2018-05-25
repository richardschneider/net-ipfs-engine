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
    public class ProtobufFormatTest
    {
        ILinkedDataFormat formatter = new ProtobufFormat();

        [TestMethod]
        public void Empty()
        {
            var data = new byte[0];
            var node = new DagNode(data);

            var cbor = formatter.Deserialise(node.ToArray());
            CollectionAssert.AreEqual(data, cbor["data"].GetByteString());
            Assert.AreEqual(0, cbor["links"].Values.Count);

            var node1 = formatter.Serialize(cbor);
            CollectionAssert.AreEqual(node.ToArray(), node1);
        }

        [TestMethod]
        public void DataOnly()
        {
            var data = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(data);

            var cbor = formatter.Deserialise(node.ToArray());
            CollectionAssert.AreEqual(data, cbor["data"].GetByteString());
            Assert.AreEqual(0, cbor["links"].Values.Count);

            var node1 = formatter.Serialize(cbor);
            CollectionAssert.AreEqual(node.ToArray(), node1);
        }

        [TestMethod]
        public void LinksOnly()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a);
            var alink = anode.ToLink("a");

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b);
            var blink = bnode.ToLink();

            var node = new DagNode(null, new[] { alink, blink });
            var cbor = formatter.Deserialise(node.ToArray());

            Assert.AreEqual(2, cbor["links"].Values.Count);

            var link = cbor["links"][0];
            Assert.AreEqual("QmYpoNmG5SWACYfXsDztDNHs29WiJdmP7yfcMd7oVa75Qv", link["Cid"]["/"].AsString());
            Assert.AreEqual("", link["Name"].AsString());
            Assert.AreEqual(3, link["Size"].AsInt32());

            link = cbor["links"][1];
            Assert.AreEqual("QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", link["Cid"]["/"].AsString());
            Assert.AreEqual("a", link["Name"].AsString());
            Assert.AreEqual(3, link["Size"].AsInt32());

            var node1 = formatter.Serialize(cbor);
            CollectionAssert.AreEqual(node.ToArray(), node1);
        }
    }
}
