using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace Ipfs.Engine
{
    
    [TestClass]
    public class IpfsEngineTest
    {
        [TestMethod]
        public void Can_Create()
        {
            var ipfs = new IpfsEngine();
            Assert.IsNotNull(ipfs);
        }

    }
}
