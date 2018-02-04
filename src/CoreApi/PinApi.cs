using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IEnumerable<Cid>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                var pins = await repo.BlockInfos
                    .Where(b => b.Pinned)
                    .ToArrayAsync(cancel);
                return pins.Select(pin => (Cid)pin.Cid);
            }
        }

        public Task<IEnumerable<Cid>> RemoveAsync(Cid id, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
