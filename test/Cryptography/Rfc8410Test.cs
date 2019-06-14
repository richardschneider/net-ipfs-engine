using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine.Cryptography
{
    [TestClass]
    public class Rfc8410Test
    {
        [TestMethod]
        public async Task ReadPrivateKey()
        {
            var ipfs = TestFixture.Ipfs;
            var keychain = await ipfs.KeyChainAsync();
            string alice1 = @"-----BEGIN PRIVATE KEY-----
MC4CAQAwBQYDK2VwBCIEINTuctv5E1hK1bbY8fdp+K06/nwoy/HU++CXqI9EdVhC
-----END PRIVATE KEY-----
";
            var key = await keychain.ImportAsync("alice1", alice1, null);
            try
            {
                var priv = (Ed25519PrivateKeyParameters) await keychain.GetPrivateKeyAsync("alice1");
                Assert.IsTrue(priv.IsPrivate);
                Assert.AreEqual("d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842", priv.GetEncoded().ToHexString());

                var pub = priv.GeneratePublicKey();
                Assert.IsFalse(pub.IsPrivate);
                Assert.AreEqual("19bf44096984cdfe8541bac167dc3b96c85086aa30b6b6cb0c5c38ad703166e1", pub.GetEncoded().ToHexString());
            }
            finally
            {
                await ipfs.Key.RemoveAsync("alice1");
            }
        }

        [TestMethod]
        public async Task ReadPrivateAndPublicKey()
        {
            var ipfs = TestFixture.Ipfs;
            var keychain = await ipfs.KeyChainAsync();
            string alice1 = @"-----BEGIN PRIVATE KEY-----
MHICAQEwBQYDK2VwBCIEINTuctv5E1hK1bbY8fdp+K06/nwoy/HU++CXqI9EdVhC
oB8wHQYKKoZIhvcNAQkJFDEPDA1DdXJkbGUgQ2hhaXJzgSEAGb9ECWmEzf6FQbrB
Z9w7lshQhqowtrbLDFw4rXAxZuE=
-----END PRIVATE KEY-----
";
            var key = await keychain.ImportAsync("alice1", alice1, null);
            try
            {
                var priv = (Ed25519PrivateKeyParameters)await keychain.GetPrivateKeyAsync("alice1");
                Assert.IsTrue(priv.IsPrivate);
                Assert.AreEqual("d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842", priv.GetEncoded().ToHexString());

                var pub = priv.GeneratePublicKey();
                Assert.IsFalse(pub.IsPrivate);
                Assert.AreEqual("19bf44096984cdfe8541bac167dc3b96c85086aa30b6b6cb0c5c38ad703166e1", pub.GetEncoded().ToHexString());
            }
            finally
            {
                await ipfs.Key.RemoveAsync("alice1");
            }
        }

    }
}
