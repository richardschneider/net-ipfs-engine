using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class BlockApi : IBlockApi
    {
        IpfsEngine ipfs;

        public BlockApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Cid> PutAsync(byte[] data, string contentType = "dag-pb", string multiHash = "sha2-256", CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Cid> PutAsync(Stream data, string contentType = "dag-pb", string multiHash = "sha2-256", CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Cid> RemoveAsync(Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IDataBlock> StatAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
