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

        public async Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default(CancellationToken))
        {
            var dht = await ipfs.DhtService;
            return await dht.FindPeerAsync(id, cancel);
        }

        public async Task<IEnumerable<Peer>> FindProvidersAsync(Cid id, int limit = 20, CancellationToken cancel = default(CancellationToken))
        {
            var dht = await ipfs.DhtService;
            return await dht.FindProvidersAsync(id, limit, cancel);
        }
    }
}
