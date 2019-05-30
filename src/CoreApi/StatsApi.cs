using System;
using System.Collections.Generic;
using System.IO;
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
            return Task.FromResult(StatsStream.AllBandwidth);
        }

        public async Task<BitswapData> BitswapAsync(CancellationToken cancel = default(CancellationToken))
        {
            var bitswap = await ipfs.BitswapService.ConfigureAwait(false);
            return bitswap.Statistics;
        }

        public Task<RepositoryData> RepositoryAsync(CancellationToken cancel = default(CancellationToken))
        {
            return ipfs.BlockRepository.StatisticsAsync(cancel);
        }
    }
}
