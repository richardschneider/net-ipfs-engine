using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class BitswapApi : IBitswapApi
    {
        IpfsEngine ipfs;

        public BitswapApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var bs = await ipfs.BitswapService;
            var peer = await ipfs.LocalPeer;
            return await bs.Want(id, peer.Id, cancel);
        }

        public async Task<IEnumerable<Cid>> WantsAsync(MultiHash peer = null, CancellationToken cancel = default(CancellationToken))
        {
            if (peer == null)
            {
                peer = (await ipfs.LocalPeer).Id;
            }
            return (await ipfs.BitswapService).PeerWants(peer);
        }
    }
}
