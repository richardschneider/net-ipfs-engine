using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class BitswapApi : IBitswapApi
    {
        IpfsEngine ipfs;

        public BitswapApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Cid>> WantsAsync(MultiHash peer = null, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
