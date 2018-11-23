using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine
{

    [TestClass]
    public class NameApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;

        [TestMethod]
        public async Task Resolve_DnsLink()
        {
            var iopath = await ipfs.Name.ResolveAsync("ipfs.io");
            Assert.IsNotNull(iopath);

            var path = await ipfs.Name.ResolveAsync("/ipns/ipfs.io");
            Assert.AreEqual(iopath, path);
        }

        [TestMethod]
        public async Task Resolve_DnsLink_Recursive()
        {
            var path = await ipfs.Name.ResolveAsync("/ipns/ipfs.io/media", true);
            StringAssert.StartsWith(path, "/ipfs/");
            StringAssert.EndsWith(path, "/media");

            path = await ipfs.Name.ResolveAsync("ipfs.io/media", true);
            StringAssert.StartsWith(path, "/ipfs/");
            StringAssert.EndsWith(path, "/media");

            path = await ipfs.Name.ResolveAsync("/ipfs.io/media", true);
            StringAssert.StartsWith(path, "/ipfs/");
            StringAssert.EndsWith(path, "/media");
        }

        [TestMethod]
        public void Resolve_NoDnsLink()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ipfs.Dns.ResolveAsync("google.com").Result;
            });
        }
    }
}
