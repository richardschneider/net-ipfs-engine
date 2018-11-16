using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PeerTalk.Cryptography
{
    [TestClass]
    public class StretchedKeyTest
    {
        // from https://github.com/libp2p/js-libp2p-crypto/blob/ad478454d86787fffed30730605d6c76a36b4d61/test/fixtures/go-stretch-key.js
        [TestMethod]
        public void StretchedSecret()
        {
            var cipher = "AES-256";
            var hash = "SHA256";
            var secret = new byte[] { 195, 191, 209, 165, 209, 201, 127, 122, 136, 111, 31, 66, 111, 68, 38, 155, 216, 204, 46, 181, 200, 188, 170, 204, 104, 74, 239, 251, 173, 114, 222, 234 };
            StretchedKey.Generate(cipher, hash, secret, out StretchedKey k1, out StretchedKey k2);

            Assert.IsNotNull(k1);
            CollectionAssert.AreEqual(new byte[] { 208, 132, 203, 169, 253, 52, 40, 83, 161, 91, 17, 71, 33, 136, 67, 96 }, k1.IV);
            CollectionAssert.AreEqual(new byte[] { 156, 48, 241, 157, 92, 248, 153, 186, 114, 127, 195, 114, 106, 104, 215, 133, 35, 11, 131, 137, 123, 70, 74, 26, 15, 60, 189, 32, 67, 221, 115, 137 }, k1.CipherKey);
            CollectionAssert.AreEqual(new byte[] { 6, 179, 91, 245, 224, 56, 153, 120, 77, 140, 29, 5, 15, 213, 187, 65, 137, 230, 202, 120 }, k1.MacKey);

            Assert.IsNotNull(k2);
            CollectionAssert.AreEqual(new byte[] { 236, 17, 34, 141, 90, 106, 197, 56, 197, 184, 157, 135, 91, 88, 112, 19 }, k2.IV);
            CollectionAssert.AreEqual(new byte[] { 151, 145, 195, 219, 76, 195, 102, 109, 187, 231, 100, 150, 132, 245, 251, 130, 254, 37, 178, 55, 227, 34, 114, 39, 238, 34, 2, 193, 107, 130, 32, 87 }, k2.CipherKey);
            CollectionAssert.AreEqual(new byte[] { 3, 229, 77, 212, 241, 217, 23, 113, 220, 126, 38, 255, 18, 117, 108, 205, 198, 89, 1, 236 }, k2.MacKey);
        }
    }
}
