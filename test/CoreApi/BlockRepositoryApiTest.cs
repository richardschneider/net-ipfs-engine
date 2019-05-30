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
    public class BlockRepositoryApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;

        [TestMethod]
        public void Exists()
        {
            Assert.IsNotNull(ipfs.BlockRepository);
        }

        [TestMethod]
        public async Task Stats()
        {
            var stats = await ipfs.BlockRepository.StatisticsAsync();
            var version = await ipfs.BlockRepository.VersionAsync();
            Assert.AreEqual(stats.Version, version);
        }

    }
}
