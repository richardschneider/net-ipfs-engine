using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{

    [TestClass]
    public class KeyApiTest
    {

        [TestMethod]
        public void Api_Exists()
        {
            var ipfs = TestFixture.Ipfs;
            Assert.IsNotNull(ipfs.Key);
        }

        [TestMethod]
        public async Task Self_Key_Exists()
        {
            var ipfs = TestFixture.Ipfs;
            var keys = await ipfs.Key.ListAsync();
            var self = keys.Single(k => k.Name == "self");
            var me = await ipfs.Generic.IdAsync();
            Assert.AreEqual("self", self.Name);
            Assert.AreEqual(me.Id, self.Id);
        }

        [TestMethod]
        public async Task Export_Import()
        {
            var password = "password".ToCharArray();
            var ipfs = TestFixture.Ipfs;
            var pem = await ipfs.Key.ExportAsync("self", password);
            StringAssert.StartsWith(pem, "-----BEGIN ENCRYPTED PRIVATE KEY-----");

            var keys = await ipfs.Key.ListAsync();
            var self = keys.Single(k => k.Name == "self");

            var clone = await ipfs.Key.ImportAsync("clone", pem, password);
            Assert.AreEqual("clone", clone.Name);
            Assert.AreEqual(self.Id, clone.Id);
        }

        [TestMethod]
        public void Export_Unknown_Key()
        {
            var password = "password".ToCharArray();
            var ipfs = TestFixture.Ipfs;
            ExceptionAssert.Throws<Exception>(() => { var x = ipfs.Key.ExportAsync("unknow", password).Result; });
        }

        [TestMethod]
        public async Task Import_Wrong_Password()
        {
            var password = "password".ToCharArray();
            var ipfs = TestFixture.Ipfs;
            var pem = await ipfs.Key.ExportAsync("self", password);

            var wrong = "wrong password".ToCharArray();
            ExceptionAssert.Throws<UnauthorizedAccessException>(() => 
            {
                var x = ipfs.Key.ImportAsync("clone", pem, wrong).Result;
            });
        }


        [TestMethod]
        public async Task Create_RSA_Key()
        {
            var name = "net-engine-test-create";
            var ipfs = TestFixture.Ipfs;
            var key = await ipfs.Key.CreateAsync(name, "rsa", 2048);
            try
            {
                Assert.IsNotNull(key);
                Assert.IsNotNull(key.Id);
                Assert.AreEqual(name, key.Name);

                var keys = await ipfs.Key.ListAsync();
                var clone = keys.Single(k => k.Name == name);
                Assert.AreEqual(key.Name, clone.Name);
                Assert.AreEqual(key.Id, clone.Id);
            }
            finally
            {
                await ipfs.Key.RemoveAsync(name);
            }
        }

        [TestMethod]
        public async Task Create_Bitcoin_Key()
        {
            var name = "test-bitcoin";
            var ipfs = TestFixture.Ipfs;
            var key = await ipfs.Key.CreateAsync(name, "secp256k1", 0);
            try
            {
                Assert.IsNotNull(key);
                Assert.IsNotNull(key.Id);
                Assert.AreEqual(name, key.Name);

                var keys = await ipfs.Key.ListAsync();
                var clone = keys.Single(k => k.Name == name);
                Assert.AreEqual(key.Name, clone.Name);
                Assert.AreEqual(key.Id, clone.Id);
            }
            finally
            {
                await ipfs.Key.RemoveAsync(name);
            }
        }

        [TestMethod]
        public async Task Remove_Key()
        {
            var name = "net-engine-test-remove";
            var ipfs = TestFixture.Ipfs;
            var key = await ipfs.Key.CreateAsync(name, "rsa", 2048);
            var keys = await ipfs.Key.ListAsync();
            var clone = keys.Single(k => k.Name == name);
            Assert.IsNotNull(clone);

            var removed = await ipfs.Key.RemoveAsync(name);
            Assert.IsNotNull(removed);
            Assert.AreEqual(key.Name, removed.Name);
            Assert.AreEqual(key.Id, removed.Id);

            keys = await ipfs.Key.ListAsync();
            Assert.IsFalse(keys.Any(k => k.Name == name));
        }

    }
}
