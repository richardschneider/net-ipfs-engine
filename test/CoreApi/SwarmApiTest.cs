using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ipfs.Engine
{

    [TestClass]
    public class SwarmApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;
        MultiAddress somewhere = "/ip4/127.0.0.1";

        [TestMethod]
        public async Task Filter_Add_Remove()
        {
            var addr = await ipfs.Swarm.AddAddressFilterAsync(somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual(somewhere, addr);
            var addrs = await ipfs.Swarm.ListAddressFiltersAsync();
            Assert.IsTrue(addrs.Any(a => a == somewhere));

            addr = await ipfs.Swarm.RemoveAddressFilterAsync(somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual(somewhere, addr);
            addrs = await ipfs.Swarm.ListAddressFiltersAsync();
            Assert.IsFalse(addrs.Any(a => a == somewhere));
        }

    }
}
