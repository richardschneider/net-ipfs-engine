using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ipfs.Engine
{
    static class TestFixture
    {
        const string passphrase = "this is not a secure pass phrase";
        public static IpfsEngine Ipfs = new IpfsEngine(passphrase.ToCharArray());

        static TestFixture()
        {
            Ipfs.Options.Repository.Folder = @"\tmp\ipfs";
        }
    }
}
