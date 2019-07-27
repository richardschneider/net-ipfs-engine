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
    class BlockRepositoryApi : IBlockRepositoryApi
    {
        IpfsEngine ipfs;

        public BlockRepositoryApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task RemoveGarbageAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<RepositoryData> StatisticsAsync(CancellationToken cancel = default(CancellationToken))
        {
            var data = new RepositoryData
            {
                RepoPath = Path.GetFullPath(ipfs.Options.Repository.Folder),
                Version = "1",
                StorageMax = 10000000000 // TODO: there is no storage max
            };

            var blockApi = (BlockApi)ipfs.Block;
            GetDirStats(blockApi.Store.Folder, data, cancel);

            return Task.FromResult(data);
        }

        public Task VerifyAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task<string> VersionAsync(CancellationToken cancel = default(CancellationToken))
        {
            var stats = await StatisticsAsync(cancel).ConfigureAwait(false);
            return stats.Version;
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
