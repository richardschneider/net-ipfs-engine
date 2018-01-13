using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class GenericApi : IGenericApi
    {
        IpfsEngine ipfs;

        public GenericApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<Peer> IdAsync(MultiHash peer = null, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, string>> VersionAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
