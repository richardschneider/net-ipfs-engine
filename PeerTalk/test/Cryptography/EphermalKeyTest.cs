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
    public class EphermalKeyTest
    {
        [TestMethod]
        public void SharedSecret()
        {
            var curve = "P-256";
            var alice = EphermalKey.Generate(curve);
            var bob = EphermalKey.Generate(curve);

            var aliceSecret = alice.GenerateSharedSecret(bob);
            var bobSecret = bob.GenerateSharedSecret(alice);
            CollectionAssert.AreEqual(aliceSecret, bobSecret);
            Assert.AreEqual(32, aliceSecret.Length);
        }

    }
}
