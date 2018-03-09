using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class ObjectApi : IObjectApi
    {
        IpfsEngine ipfs;

        public ObjectApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<Stream> DataAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task<DagNode> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var block = await ipfs.Block.GetAsync(id, cancel);
            return new DagNode(block.DataStream);
        }

        public Task<IEnumerable<IMerkleLink>> LinksAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DagNode> NewAsync(string template = null, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DagNode> NewDirectoryAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DagNode> PutAsync(byte[] data, IEnumerable<IMerkleLink> links = null, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DagNode> PutAsync(DagNode node, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
