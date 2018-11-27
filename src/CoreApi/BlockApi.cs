using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Common.Logging;
using System.Linq;
using System.Runtime.Serialization;

namespace Ipfs.Engine.CoreApi
{
    [DataContract]
    class DataBlock : IDataBlock
    {
        [DataMember]
        public byte[] DataBytes { get; set; }

        public Stream DataStream { get { return new MemoryStream(DataBytes, false); } }

        [DataMember]
        public Cid Id  { get; set; }

        [DataMember]
        public long Size { get; set; }
    }

    class BlockApi : IBlockApi
    {
        static ILog log = LogManager.GetLogger(typeof(BlockApi));
        static DataBlock emptyDirectory = new DataBlock
        {
            DataBytes = ObjectApi.EmptyDirectory.ToArray(),
            Id = ObjectApi.EmptyDirectory.Id,
            Size = ObjectApi.EmptyDirectory.ToArray().Length
        };
        static DataBlock emptyNode = new DataBlock
        {
            DataBytes = ObjectApi.EmptyNode.ToArray(),
            Id = ObjectApi.EmptyNode.Id,
            Size = ObjectApi.EmptyNode.ToArray().Length
        };

        IpfsEngine ipfs;
        FileStore<Cid, DataBlock> store;

        public BlockApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        FileStore<Cid, DataBlock> Store
        {
            get
            {
                if (store == null)
                {
                    var folder = Path.Combine(ipfs.Options.Repository.Folder, "blocks");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    store = new FileStore<Cid, DataBlock>
                    {
                        Folder = folder,
                        NameToKey = (cid) => cid.Hash.ToBase32(),
                        KeyToName = (key) => new MultiHash(key.FromBase32()),
                        Serialize = async (stream, cid, block, cancel) => 
                        {
                            await stream.WriteAsync(block.DataBytes, 0, block.DataBytes.Length, cancel);
                        },
                        Deserialize = async (stream, cid, cancel) =>
                        {
                            var block = new DataBlock
                            {
                                Id = cid,
                                Size = stream.Length
                            };
                            block.DataBytes = new byte[block.Size];
                            for (int i = 0, n; i < block.Size; i += n)
                            {
                                n = await stream.ReadAsync(block.DataBytes, i, (int)block.Size - i, cancel);
                            }
                            return block;
                        }
                    };
                }
                return store;
            }
        }


        public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            // Hack for empty object and empty directory object
            if (id == emptyDirectory.Id)
                return emptyDirectory;
            if (id == emptyNode.Id)
                return emptyNode;

            // If identity hash, then CID has the content.
            if (id.Hash.IsIdentityHash)
            {
                return new DataBlock
                {
                    DataBytes = id.Hash.Digest,
                    Id = id,
                    Size = id.Hash.Digest.Length
                };
            }

            // Check the local filesystem for the block.
            var block = await Store.TryGetAsync(id, cancel);
            if (block != null)
            {
                return block;
            }

            // Query the network, via DHT, for peers that can provide the
            // content.  As a provider peer is found, it is connected to and
            // the bitswap want lists are exchanged.  Hopefully the provider will
            // then send the block to us via bitswap and the get task will finish.
            using (var queryCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel))
            {
                var bitswapGet = ipfs.Bitswap.GetAsync(id, cancel);
                var dht = await ipfs.DhtService;
                var _ = dht.FindProvidersAsync(
                    id: id,
                    limit: 20, // TODO: remove this
                    cancel: queryCancel.Token,
                    action: ProviderFound
                );

                var got = await bitswapGet;

                queryCancel.Cancel(); // stop the network query.
                return got;
            }
        }

        async void ProviderFound(Peer peer)
        {
            log.Debug($"Connecting to provider {peer.Id}");
            var swarm = await ipfs.SwarmService;
            try
            {
                await swarm.ConnectAsync(peer);
            }
            catch (Exception e)
            {
                log.Warn($"Connection to provider {peer.Id} failed, {e.Message}");
            }
        }

        public async Task<Cid> PutAsync(
            byte[] data,
            string contentType = Cid.DefaultContentType,
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = false,
            CancellationToken cancel = default(CancellationToken))
        {
            // Small enough for an inline CID?
            if (ipfs.Options.Block.AllowInlineCid && data.Length <= ipfs.Options.Block.InlineCidLimit)
            {
                return new Cid
                {
                    ContentType = contentType,
                    Hash = MultiHash.ComputeHash(data, "identity")
                };
            }

            var cid = new Cid
            {
                ContentType = contentType,
                Encoding = encoding,
                Hash = MultiHash.ComputeHash(data, multiHash)
            };
            var block = new DataBlock
            {
                DataBytes = data,
                Id = cid,
                Size = data.Length
            };
            if (await Store.ExistsAsync(cid))
            {
                log.DebugFormat("Block '{0}' already present", cid);
            }
            else
            {
                await Store.PutAsync(cid, block, cancel);
                log.DebugFormat("Added block '{0}'", cid);
            }

            // Inform the Bitswap service.
            (await ipfs.BitswapService).Found(block);

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

        public async Task<Cid> PutAsync(
            Stream data,
            string contentType = Cid.DefaultContentType,
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = false,
            CancellationToken cancel = default(CancellationToken))
        {
            using (var ms = new MemoryStream())
            {
                await data.CopyToAsync(ms);
                return await PutAsync(ms.ToArray(), contentType, multiHash, encoding, pin, cancel);
            }
        }

        public async Task<Cid> RemoveAsync(Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default(CancellationToken))
        {
            if (id.Hash.IsIdentityHash)
            {
                return id;
            }
            if (await Store.ExistsAsync(id, cancel))
            {
                await Store.RemoveAsync(id, cancel);
                await ipfs.Pin.RemoveAsync(id, recursive: false, cancel: cancel);
                return id;
            }
            if (ignoreNonexistent) return null;
            throw new KeyNotFoundException($"Block '{id.Encode()}' does not exist.");
        }

        public async Task<IDataBlock> StatAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            if (id.Hash.IsIdentityHash)
            {
                return await GetAsync(id, cancel);
            }

            IDataBlock block = null;
            var length = await Store.LengthAsync(id, cancel);
            if (length.HasValue)
            {
                block = new DataBlock
                {
                    Id = id,
                    Size = length.Value
                };
            }

            return block;
        }
    }
}
