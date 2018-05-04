using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class DnsApi : IDnsApi
    {
        IpfsEngine ipfs;

        public DnsApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<string> ResolveAsync(string name, bool recursive = false, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
