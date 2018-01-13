using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class SwarmApi : ISwarmApi
    {
        IpfsEngine ipfs;

        public SwarmApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<IEnumerable<Peer>> AddressesAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task ConnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task DisconnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Peer>> PeersAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
