using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Ipfs.Engine
{
    [TestClass]
    public class TestFixture
    {
        const string passphrase = "this is not a secure pass phrase";
        public static IpfsEngine Ipfs = new IpfsEngine(passphrase.ToCharArray());
        public static IpfsEngine IpfsOther = new IpfsEngine(passphrase.ToCharArray());

        static TestFixture()
        {
            Ipfs.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), "ipfs-test");
            Ipfs.Options.KeyChain.DefaultKeySize = 512;
            Ipfs.Config.SetAsync(
                "Addresses.Swarm", 
                JToken.FromObject(new string[] { "/ip4/0.0.0.0/tcp/4007" })
            ).Wait();

            IpfsOther.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), "ipfs-other");
            IpfsOther.Options.KeyChain.DefaultKeySize = 512;
            IpfsOther.Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] { "/ip4/0.0.0.0/tcp/4008" })
            ).Wait();
        }

        [TestMethod]
        public void Engine_Exists()
        {
            Assert.IsNotNull(Ipfs);
            Assert.IsNotNull(IpfsOther);
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            if (Directory.Exists(Ipfs.Options.Repository.Folder))
            {
                Directory.Delete(Ipfs.Options.Repository.Folder, true);
            }
            if (Directory.Exists(IpfsOther.Options.Repository.Folder))
            {
                Directory.Delete(IpfsOther.Options.Repository.Folder, true);
            }
        }
    }
}
