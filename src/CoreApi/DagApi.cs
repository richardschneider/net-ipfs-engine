using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class DagApi : IDagApi
    {
        IpfsEngine ipfs;

        public DagApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<ILinkedNode> GetAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Cid> PutAsync(ILinkedNode data, string contentType, string multiHash = "sha2-256", CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
