using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{

    [TestClass]
    public class StatsApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;

        [TestMethod]
        public void Exists()
        {
            Assert.IsNotNull(ipfs.Stats);
        }

    }
}
