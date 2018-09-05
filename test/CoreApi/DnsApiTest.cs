using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine
{

    [TestClass]
    public class DnsApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;

        [TestMethod]
        public async Task Resolve()
        {
            var path = await ipfs.Dns.ResolveAsync("ipfs.io");
            Assert.IsNotNull(path);
        }

        [TestMethod]
        public void Resolve_NoLink()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ipfs.Dns.ResolveAsync("google.com").Result;
            });
        }
    }
}
