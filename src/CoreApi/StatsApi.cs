using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Concurrent;
using PeerTalk;

namespace Ipfs.Engine.CoreApi
{
    class StatsApi : IStatsApi
    {
        IpfsEngine ipfs;

        public StatsApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<BandwidthData> BandwidthAsync(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(new BandwidthData()); // TODO
        }

        public Task<BitswapData> BitswapAsync(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(new BitswapData()); // TODO
        }

        public Task<RepositoryData> RepositoryAsync(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(new RepositoryData()); // TODO
        }
    }
}
