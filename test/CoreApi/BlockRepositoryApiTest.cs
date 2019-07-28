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

        [TestMethod]
        public async Task GarbageCollection()
        {
            var pinned = await ipfs.Block.PutAsync(new byte[256], pin: true);
            var unpinned = await ipfs.Block.PutAsync(new byte[512], pin: false);
            Assert.AreNotEqual(pinned, unpinned);
            Assert.IsNotNull(await ipfs.Block.StatAsync(pinned));
            Assert.IsNotNull(await ipfs.Block.StatAsync(unpinned));

            await ipfs.BlockRepository.RemoveGarbageAsync();
            Assert.IsNotNull(await ipfs.Block.StatAsync(pinned));
            Assert.IsNull(await ipfs.Block.StatAsync(unpinned));
        }

    }
}
