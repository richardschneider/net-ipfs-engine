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
        public static IpfsEngine Ipfs = new IpfsEngine();

        static TestFixture()
        {
            Ipfs.Options.Repository.Folder = @"\tmp\ipfs";
        }
    }
}
