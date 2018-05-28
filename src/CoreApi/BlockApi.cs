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
        string blocksFolder;

        public BlockApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        string BlocksFolder
        {
            get
            {
                if (blocksFolder == null)
                {
                    blocksFolder = Path.Combine(ipfs.Options.Repository.Folder, "blocks");
                    if (!Directory.Exists(blocksFolder))
                    {
                        log.DebugFormat("creating folder '{0}'", blocksFolder);
                        Directory.CreateDirectory(blocksFolder);
                    }
                }
                return blocksFolder;
            }
        }

        /// <summary>
        ///   Local file system path of the content ID.
        /// </summary>
        /// <param name="id">
        ///   The conten ID.
        /// </param>
        /// <returns>
        ///   The path to the <paramref name="id"/>.
        /// </returns>
        /// <remarks>
        ///   To support case insenstive file systems, the content ID's multihash value
        ///   is z-base-32 encoded.
        /// </remarks>
        string GetPath(Cid id)
        {
            return Path.Combine(
                BlocksFolder, 
                Base32z.Codec.Encode(id.Hash.Digest, false));
        }

        public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var contentPath = GetPath(id);
            if (File.Exists(contentPath))
            {
                var block = new DataBlock
                {
                    Id = id,
                    Size = new FileInfo(contentPath).Length
                };
                block.DataBytes = new byte[block.Size];
                using (var content = File.OpenRead(contentPath))
                {
                    for (int i = 0, n; i < block.Size; i += n)
                    {
                        n = await content.ReadAsync(block.DataBytes, i, (int)block.Size - i, cancel);
                    }
                }
                return block;
            }

            // TODO: Let bitswap find it.
            throw new NotImplementedException("Need bitswap to fetch the block.");
        }

        public async Task<Cid> PutAsync(byte[] data, string contentType = "dag-pb", string multiHash = "sha2-256", bool pin = false, CancellationToken cancel = default(CancellationToken))
        {
            var cid = new Cid
            {
                ContentType = contentType,
                Hash = MultiHash.ComputeHash(data, multiHash),
                Version = (contentType == "dag-pb" && multiHash == "sha2-256") ? 0 : 1
            };
            var contentPath = GetPath(cid);
            if (File.Exists(contentPath))
            {
                log.DebugFormat("Block '{0}' already present", cid);
            }
            else
            {
                using (var stream = File.Create(contentPath))
                {
                    await stream.WriteAsync(data, 0, data.Length, cancel);
                }
                log.DebugFormat("Added block '{0}'", cid);
            }
            
            // To pin or not.
            if (pin)
            {
                await ipfs.Pin.AddAsync(cid, recursive: false, cancel: cancel);
            }
            else
            {
                await ipfs.Pin.RemoveAsync(cid, recursive: false, cancel: cancel);
            }

            return cid;
        }

        public async Task<Cid> PutAsync(Stream data, string contentType = "dag-pb", string multiHash = "sha2-256", bool pin = false, CancellationToken cancel = default(CancellationToken))
        {
            using (var ms = new MemoryStream())
            {
                await data.CopyToAsync(ms);
                return await PutAsync(ms.ToArray(), contentType, multiHash, pin, cancel);
            }
        }

        public async Task<Cid> RemoveAsync(Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default(CancellationToken))
        {
            var contentPath = GetPath(id);
            if (File.Exists(contentPath))
            {
                File.Delete(contentPath);
                await ipfs.Pin.RemoveAsync(id, recursive: false, cancel: cancel);
                return id;
            }
            if (ignoreNonexistent) return null;
            throw new KeyNotFoundException($"Block '{id}' does not exist.");
        }

        public Task<IDataBlock> StatAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            IDataBlock block = null;
            var contentPath = GetPath(id);
            if (File.Exists(contentPath))
            {
                block = new DataBlock
                {
                    Id = id,
                    Size = new FileInfo(contentPath).Length
                };
            }
            return Task.FromResult(block);
        }
    }
}
