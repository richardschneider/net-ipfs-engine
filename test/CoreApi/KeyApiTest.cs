using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
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

            await ipfs.Key.RemoveAsync("clone");
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
        public async Task Import_JSIPFS_Node()
        {
            string pem = @"-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIFDTA/BgkqhkiG9w0BBQ0wMjAaBgkqhkiG9w0BBQwwDQQILdGJynKmkrMCAWQw
FAYIKoZIhvcNAwcECByaxdAET2tuBIIEyCKPITRayWR57HOJeTooJVR4tFCaNIo+
ThspwXbk+EqkhQUOcmn+OrgizxL9/sX1l+VlZYR9NkWqbaKo9yeZCX79p64MvUvp
IplgXxEf+rdfZ5xPQKN2Rfv7DyHW5h0JKMEISSLpgtA4Pc0Sr7PQdkLCS0tIF8yL
FEo7YA+yrmsUQbIeVabMLG0DaN2/csydp26IfldAOjqQgy5YIG/xz6gtISfXquxZ
YLPghpM4l/vK2IgxTbKPXQCLJ34rVRvIulIe0Zs+a9Om9O0uLZtTcM0VzlbIH6H7
fJlx6poxkBIG/Ui3nIjiOHa5DnqAxCNxkRH9TEBjoqFkPYQ1ExvfIIic2JqT8JUO
nKX9vGuudS/MqAEUO8EvrI68F4E+7zc/ahh/S3PQVhMZuR8ajblUZUxnItXgFt/0
mnOca3HNB2hLz83ubBvr6E9Nt/7AddIfaZkuIXkrXmz7LfelIsslUk4YIy7YchMv
Z1heLasChKVL7GEWoqXBv1erks+l8tpTe/iS/d4sWT8AiTFfPZu53TZ98vGtjLNO
RdaTNYP3tWyWCwQAPchHF9wLHCFjTrC2gsYgqalv2tYhHSqQg5lhDi9u+Z7bMihT
WzXVp7ddkYXX7wgD6yuPQWeKgkIlfKjiHMs6sfc5UBVEJWlgDtTl6DdUa0LaqGWF
J3b6Pc0f1NXNPN+iZlhO50eBunPUd0HT1tbdIwM9sqdDOe8+O55kUcXVeQAqPgj+
Qo0wi+L6bGSIhGXnoYPNn9al0ANNYV2Y2KJGPiWVEq8eLt3pFlhb3ELCEjgno9Sf
lYyIrP1hy9hfoN2sFM/s2Rn7/9n5u3/Gi9hVi8VlZGa7j4LLquP52ZLYBLOypwc3
2EDHw47r2TTKIcfiEwAEpuFuZDWyD0AgL0JwKYVKnwgzZIOiAW00Xn9FWmZQBtqK
Pb7jWArvmjvBK7cc0rLfLu2x4pF2DujeDxvru047nv97xiRpCpCLe6o5PRBotKkx
YYJQ7oS0u/n9gUbz45nR1DWXS3nAMCAXmNc6GLdFpjnTm8ivnnJi+8CBQvhUAKjK
BLKkOVFpTnDc8ha0ovtqCuKG2Ou+lU3z7BLqRCIGBbsHGxIC/aT708xGR9PhvARa
/y5NvdeDylnjX9pqa/zZXnO2844UfjQkXiJ5VP07MA0z4cQ56Isp1/UbT3+KCjRz
GT0HIE7AJhamgNE46ZHfhQfXSYhKxxPW3fd9wrFs55/1wTskfJgQFRYsrROZ6eDA
NyC8CV8reE/fgQk+0N/06lQ0/apqlEOC1uhNnS7b3AX7dk16BJNHsoMFs2SuDbOa
4skeCGK0oNjrbhYH9HIPQYoPEweE8QnebzSnxf9SRz0NlqS3vqsSEnIYY9F1t05u
YiNEZC4dk3vwRn6u3Br64fyg+dpYDWn+iSSWBan5Qof7uPht7QbZKCjLTRJcJVp2
lwUw2p8yaABLqSgEZ23jH5glGX3dJ1itAxiYxcFy/8GAbd+qTvaco73nRCS7ZeL+
FTrAh+xquPCw1yhbkeFtSVuUUqxQeXi9Zyq6kbeX+56HabAWPr3bg43zecFMM4tK
7xOA2p/9gala79mMYX49kYXwiP82nfouyyeSKv2jI9+6lejo+s4Lpnj6HfDsiJhl
Rw==
-----END ENCRYPTED PRIVATE KEY-----
";
            var spki = "CAASpgIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCfBYU9c0n28u02N/XCJY8yIsRqRVO5Zw+6kDHCremt2flHT4AaWnwGLAG9YyQJbRTvWN9nW2LK7Pv3uoIlvUSTnZEP0SXB5oZeqtxUdi6tuvcyqTIfsUSanLQucYITq8Qw3IMBzk+KpWNm98g9A/Xy30MkUS8mrBIO9pHmIZa55fvclDkTvLxjnGWA2avaBfJvHgMSTu0D2CQcmJrvwyKMhLCSIbQewZd2V7vc6gtxbRovKlrIwDTmDBXbfjbLljOuzg2yBLyYxXlozO9blpttbnOpU4kTspUVJXglmjsv7YSIJS3UKt3544l/srHbqlwC5CgOgjlwNfYPadO8kmBfAgMBAAE=";
            var password = "password".ToCharArray();
            var ipfs = TestFixture.Ipfs;

            await ipfs.Key.RemoveAsync("jsipfs");
            var key = await ipfs.Key.ImportAsync("jsipfs", pem, password);
            Assert.AreEqual("jsipfs", key.Name);
            Assert.AreEqual("QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb", key.Id);

            var keychain = await ipfs.KeyChain();
            var pubkey = await keychain.GetPublicKeyAsync("jsipfs");
            Assert.AreEqual(spki, pubkey);
        }

        [TestMethod]
        public void Import_Bad_Format()
        {
            string pem = @"this is not PEM";
            var password = "password".ToCharArray();
            var ipfs = TestFixture.Ipfs;
            ExceptionAssert.Throws<InvalidDataException>(() =>
            {
                var x = ipfs.Key.ImportAsync("bad", pem, password).Result;
            });
        }

        [TestMethod]
        public void Import_Corrupted_Format()
        {
            string pem = @"-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIFDTA/BgkqhkiG9w0BBQ0wMjAaBgkqhkiG9w0BBQwwDQQILdGJynKmkrMCAWQw
-----END ENCRYPTED PRIVATE KEY-----
";
            var password = "password".ToCharArray();
            var ipfs = TestFixture.Ipfs;
            ExceptionAssert.Throws<Exception>(() =>
            {
                var x = ipfs.Key.ImportAsync("bad", pem, password).Result;
            });
        }

        [TestMethod]
        public async Task Create_RSA_Key()
        {
            var name = "net-engine-test-create";
            var ipfs = TestFixture.Ipfs;
            var key = await ipfs.Key.CreateAsync(name, "rsa", 512);
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
            var key = await ipfs.Key.CreateAsync(name, "secp256k1", 0);
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

        [TestMethod]
        public async Task Rename_Key()
        {
            var name = "net-engine-test-rename0";
            var newName = "net-engine-test-rename1";
            var ipfs = TestFixture.Ipfs;

            await ipfs.Key.RemoveAsync(name);
            await ipfs.Key.RemoveAsync(newName);
            var key = await ipfs.Key.CreateAsync(name, "secp256k1", 0);
            var renamed = await ipfs.Key.RenameAsync(name, newName);
            Assert.AreEqual(key.Id, renamed.Id);
            Assert.AreEqual(newName, renamed.Name);

            var keys = await ipfs.Key.ListAsync();
            Assert.IsTrue(keys.Any(k => k.Name == newName));
            Assert.IsFalse(keys.Any(k => k.Name == name));
        }

        [TestMethod]
        public async Task Remove_Unknown_Key()
        {
            var name = "net-engine-test-remove-unknown";
            var ipfs = TestFixture.Ipfs;

            var removed = await ipfs.Key.RemoveAsync(name);
            Assert.IsNull(removed);
        }

        [TestMethod]
        public async Task Rename_Unknown_Key()
        {
            var name = "net-engine-test-rename-unknown";
            var ipfs = TestFixture.Ipfs;

            var renamed = await ipfs.Key.RenameAsync(name, "foobar");
            Assert.IsNull(renamed);
        }

        [TestMethod]
        public void Create_Unknown_KeyType()
        {
            var ipfs = TestFixture.Ipfs;

            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ipfs.Key.CreateAsync("unknown", "unknown", 0).Result;
            });
        }
    }
}
