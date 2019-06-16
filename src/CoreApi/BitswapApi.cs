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
            var bs = await ipfs.BitswapService.ConfigureAwait(false);
            var peer = await ipfs.LocalPeer.ConfigureAwait(false);
            return await bs.WantAsync(id, peer.Id, cancel).ConfigureAwait(false);
        }

        public Task<BitswapLedger> LedgerAsync(Peer peer, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task UnwantAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            (await ipfs.BitswapService.ConfigureAwait(false)).Unwant(id);
        }

        public async Task<IEnumerable<Cid>> WantsAsync(MultiHash peer = null, CancellationToken cancel = default(CancellationToken))
        {
            if (peer == null)
            {
                peer = (await ipfs.LocalPeer.ConfigureAwait(false)).Id;
            }
            var bs = await ipfs.BitswapService.ConfigureAwait(false);
            return bs.PeerWants(peer);
        }
    }
}
