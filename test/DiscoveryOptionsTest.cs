using Ipfs.Engine.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    
    [TestClass]
    public class DiscoveryOptionsTest
    {
        [TestMethod]
        public void Defaults()
        {
            var options = new DiscoveryOptions();
            Assert.IsNull(options.BootstrapPeers);
        }

    }
}
