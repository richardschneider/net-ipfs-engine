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
            var bitswap = await ipfs.BitswapService;
            return bitswap.Statistics;
        }

        public Task<RepositoryData> RepositoryAsync(CancellationToken cancel = default(CancellationToken))
        {
            var data = new RepositoryData
            {
                RepoPath = Path.GetFullPath(ipfs.Options.Repository.Folder),
                Version = "1",
                StorageMax = 0 // TODO: there is no storage max
            };

            GetDirStats(data.RepoPath, data, cancel);

            return Task.FromResult(data);
        }

        void GetDirStats(string path, RepositoryData data, CancellationToken cancel)
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                cancel.ThrowIfCancellationRequested();
                ++data.NumObjects;
                data.RepoSize += (ulong)(new FileInfo(file).Length);
            }

            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                cancel.ThrowIfCancellationRequested();
                GetDirStats(dir, data, cancel);
            }
        }
    }
}
