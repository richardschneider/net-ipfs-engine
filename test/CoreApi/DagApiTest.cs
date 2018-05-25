using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine
{

    [TestClass]
    public class DagApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;
        byte[] blob = Encoding.UTF8.GetBytes("blorb");
        string blob64 = "YmxvcmI"; // base 64 encoded with no padding

        [TestMethod]
        public async Task Get_Raw()
        {
            var cid = await ipfs.Block.PutAsync(blob, contentType: "raw");
            Assert.AreEqual("zb2rhYDhWhxyHN6HFAKGvHnLogYfnk9KvzBUZvCg7sYhS22N8", (string)cid);

            var dag = await ipfs.Dag.GetAsync(cid);
            Assert.AreEqual(blob64, (string) dag["data"]);
        }

        class Name
        {
            public string First { get; set; }
            public string Last { get; set; }
        }

        class name
        {
            public string first { get; set; }
            public string last { get; set; }
        }

        [TestMethod]
        public async Task PutAndGet_JSON()
        {
            var expected = new JObject();
            expected["a"] = "alpha";
            var id = await ipfs.Dag.PutAsync(expected);
            Assert.IsNotNull(id);

            var actual = await ipfs.Dag.GetAsync(id);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected["a"], actual["a"]);

            var value = (string)await ipfs.Dag.GetAsync(id.Encode() + "/a");
            Assert.AreEqual(expected["a"], value);
        }

        [TestMethod]
        public async Task PutAndGet_poco()
        {
            var expected = new name { first = "John", last = "Smith" };
            var id = await ipfs.Dag.PutAsync(expected);
            Assert.IsNotNull(id);

            var actual = await ipfs.Dag.GetAsync<name>(id);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.first, actual.first);
            Assert.AreEqual(expected.last, actual.last);

            var value = (string)await ipfs.Dag.GetAsync(id.Encode() + "/last");
            Assert.AreEqual(expected.last, value);
        }

        [TestMethod]
        public async Task PutAndGet_POCO()
        {
            var expected = new Name { First = "John", Last = "Smith" };
            var id = await ipfs.Dag.PutAsync(expected);
            Assert.IsNotNull(id);

            var actual = await ipfs.Dag.GetAsync<Name>(id);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.First, actual.First);
            Assert.AreEqual(expected.Last, actual.Last);

            var value = (string)await ipfs.Dag.GetAsync(id.Encode() + "/Last");
            Assert.AreEqual(expected.Last, value);
        }

        [TestMethod]
        public async Task Get_Raw2()
        {
            var data = Encoding.UTF8.GetBytes("abc");
            var id = await ipfs.Block.PutAsync(data, "raw");
            Assert.AreEqual("zb2rhjCBERUodVMcKTiFjjbWP12nfh2nNKKcpDNHeQPReWC2G", id.Encode());

            var actual = await ipfs.Dag.GetAsync(id);
            Assert.AreEqual(Convert.ToBase64String(data), (string)actual["data"]);
        }
    }

}
