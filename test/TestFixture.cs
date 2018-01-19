using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Engine
{
    [TestClass]
    public class TestFixture
    {
        const string passphrase = "this is not a secure pass phrase";
        public static IpfsEngine Ipfs = new IpfsEngine(passphrase.ToCharArray());

        static TestFixture()
        {
            Ipfs.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), "ipfs-test");
        }

        [TestMethod]
        public void Engine_Exists()
        {
            Assert.IsNotNull(Ipfs);
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            Directory.Delete(Ipfs.Options.Repository.Folder, true);
        }
    }
}
