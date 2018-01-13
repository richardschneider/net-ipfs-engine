using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class PinApi : IPinApi
    {
        IpfsEngine ipfs;

        public PinApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<IEnumerable<Cid>> AddAsync(string path, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Cid>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Cid>> RemoveAsync(Cid id, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
