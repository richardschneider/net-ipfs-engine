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
            var dht = await ipfs.DhtService.ConfigureAwait(false);
            return await dht.FindPeerAsync(id, cancel).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Peer>> FindProvidersAsync(Cid id, int limit = 20, Action<Peer> providerFound = null, CancellationToken cancel = default(CancellationToken))
        {
            var dht = await ipfs.DhtService.ConfigureAwait(false);
            return await dht.FindProvidersAsync(id, limit, providerFound, cancel).ConfigureAwait(false);
        }

        public async Task ProvideAsync(Cid cid, bool advertise = true, CancellationToken cancel = default(CancellationToken))
        {
            var dht = await ipfs.DhtService.ConfigureAwait(false);
            await dht.ProvideAsync(cid, advertise, cancel).ConfigureAwait(false);
        }

        public Task<byte[]> GetAsync(byte[] key, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task PutAsync(byte[] key, out byte[] value, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryGetAsync(byte[] key, out byte[] value, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
