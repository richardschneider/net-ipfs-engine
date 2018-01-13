using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class NameApi : INameApi
    {
        IpfsEngine ipfs;

        public NameApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<Cid> PublishAsync(string path, bool resolve = true, string key = "self", TimeSpan? lifetime = null, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Cid> ResolveAsync(string name, bool recursive = false, bool nocache = false, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
