using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer.Protocols
{
    [TestClass]
    public class ProtocolRegistryTest
    {
        [TestMethod]
        public void PreRegistered()
        {
            CollectionAssert.Contains(ProtocolRegistry.Protocols.Keys, "/multistream/1.0.0");
            CollectionAssert.Contains(ProtocolRegistry.Protocols.Keys, "/plaintext/1.0.0");
        }

    }
}
