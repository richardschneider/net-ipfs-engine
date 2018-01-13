using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class DhtApi : IDhtApi
    {
        IpfsEngine ipfs;

        public DhtApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Peer>> FindProvidersAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
