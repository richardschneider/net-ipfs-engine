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
using System.Globalization;

namespace Ipfs.Engine.CoreApi
{
    class BlockRepositoryApi : IBlockRepositoryApi
    {
        IpfsEngine ipfs;

        public BlockRepositoryApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task RemoveGarbageAsync(CancellationToken cancel = default(CancellationToken))
        {
            var blockApi = (BlockApi)ipfs.Block;
            var pinApi = (PinApi)ipfs.Pin;
            foreach (var cid in blockApi.Store.Names)
            {
                if (!await pinApi.IsPinnedAsync(cid, cancel).ConfigureAwait(false))
                {
                    await ipfs.Block.RemoveAsync(cid, ignoreNonexistent: true, cancel: cancel).ConfigureAwait(false);
                }
            }
        }

        public async Task<RepositoryData> StatisticsAsync(CancellationToken cancel = default(CancellationToken))
        {
            var data = new RepositoryData
            {
                RepoPath = Path.GetFullPath(ipfs.Options.Repository.Folder),
                Version = await VersionAsync(cancel).ConfigureAwait(false),
                StorageMax = 10000000000 // TODO: there is no storage max
            };

            var blockApi = (BlockApi)ipfs.Block;
            GetDirStats(blockApi.Store.Folder, data, cancel);

            return data;
        }

        public Task VerifyAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<string> VersionAsync(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(ipfs.MigrationManager
                .CurrentVersion
                .ToString(CultureInfo.InvariantCulture));
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
