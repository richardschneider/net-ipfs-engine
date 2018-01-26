using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Common.Logging;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Ipfs.Engine.CoreApi
{
    class DataBlock : IDataBlock
    {
        public byte[] DataBytes { get; set; }
        public Stream DataStream { get { return new MemoryStream(DataBytes, false); } }
        public Cid Id  { get; set; }
        public long Size { get; set; }
    }

    class BlockApi : IBlockApi
    {
        static ILog log = LogManager.GetLogger(typeof(BlockApi));

        IpfsEngine ipfs;

        public BlockApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                var block = await repo.BlockValues
                    .Where(b => b.Cid == id.Encode())
                    .FirstOrDefaultAsync(cancel);
                if (block == null) // TODO: call on bitswap
                {
                    return null;
                }
                return new DataBlock
                {
                    DataBytes = block.Data,
                    Id = id,
                    Size = block.Data.Length
                };
            }
        }

        public async Task<Cid> PutAsync(byte[] data, string contentType = "dag-pb", string multiHash = "sha2-256", CancellationToken cancel = default(CancellationToken))
        {
            using (var ms = new MemoryStream(data, false))
            {
                return await PutAsync(ms, contentType, multiHash, cancel);
            }
        }

        public async Task<Cid> PutAsync(Stream data, string contentType = "dag-pb", string multiHash = "sha2-256", CancellationToken cancel = default(CancellationToken))
        {
            var cid = new Cid
            {
                ContentType = contentType,
                Hash = MultiHash.ComputeHash(data, multiHash),
                Version = (contentType == "dag-pb" && multiHash == "sha2-256") ? 0 : 1
            };

            // Store the key in the repository.
            using (var repo = await ipfs.Repository(cancel))
            {
                var block = await repo.BlockInfos
                    .Where(b => b.Cid == cid.Encode())
                    .FirstOrDefaultAsync(cancel);
                if (block != null)
                {
                    log.DebugFormat("Block '{0}' already present", cid);
                    return cid;
                }

                // TODO: Ineffecient in memory usage.  Might be better to do all
                // the work in the byte[] method.
                var bytes = new byte[data.Length];
                data.Position = 0;
                data.Read(bytes, 0, (int)data.Length);
                var blockInfo = new Repository.BlockInfo
                {
                    Cid = cid,
                    Pinned = false,
                    DataSize = data.Length
                };
                var blockValue = new Repository.BlockValue
                {
                    Cid = cid,
                    Data = bytes
                };
                await repo.AddAsync(blockInfo, cancel);
                await repo.AddAsync(blockValue, cancel);
                await repo.SaveChangesAsync(cancel);

                log.DebugFormat("Added block '{0}'", cid);
            }

            // TODO: Send to bitswap
            return cid;
        }

        public async Task<Cid> RemoveAsync(Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                var pk = new string[] { id };
                var blockInfo = await repo.BlockInfos.FindAsync(pk, cancel);
                if (blockInfo != null)
                {
                    repo.BlockInfos.Remove(blockInfo);
                    var value = await repo.BlockValues.FindAsync(pk, cancel);
                    repo.BlockValues.Remove(value);
                    await repo.SaveChangesAsync(cancel);
                }
                if (blockInfo != null) return id;
                if (ignoreNonexistent) return null;
                throw new KeyNotFoundException($"Block '{id}' does not exist.");
            }
        }

        public async Task<IDataBlock> StatAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                var block = await repo.BlockInfos
                    .Where(b => b.Cid == id.Encode())
                    .FirstOrDefaultAsync(cancel);
                if (block == null) // TODO: call on bitswap
                {
                    return null;
                }
                return new DataBlock
                {
                    Id = id,
                    Size = block.DataSize
                };
            }
        }
    }
}
