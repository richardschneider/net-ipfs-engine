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

        public async Task<IEnumerable<Cid>> AddAsync(string path, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            var id = await ipfs.ResolveIpfsPathToCidAsync(path, cancel);
            var todos = new Stack<Cid>();
            todos.Push(id);
            var dones = new List<Cid>();

            // The pin is added before the content is fetched, so that
            // garbage collection will not delete the newly pinned
            // content.

            while (todos.Count > 0)
            {
                var current = todos.Pop();

                // Add CID to PIN database.
                using (var repo = await ipfs.Repository(cancel))
                {
                    var cid = current.Encode();
                    try
                    {
                        repo.Add(new Repository.Pin { Cid = cid });
                        await repo.SaveChangesAsync(cancel);
                    }
                    catch (DbUpdateException)
                    {
                        // Already pinned is okay.
                    }
                }

                // Make sure that the content is stored locally.
                await ipfs.Block.GetAsync(current, cancel);

                // Recursively pin the links?
                if (recursive)
                {
                    var links = await ipfs.Object.LinksAsync(current, cancel);
                    foreach (var link in links)
                    {
                        todos.Push(link.Id);
                    }
                }
                dones.Add(current);
            }

            return dones;
        }

        public async Task<IEnumerable<Cid>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                var pins = await repo.Pins.ToArrayAsync(cancel);
                return pins.Select(pin => (Cid)pin.Cid);
            }
        }

        public async Task<IEnumerable<Cid>> RemoveAsync(Cid id, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            var todos = new Stack<Cid>();
            todos.Push(id);
            var dones = new List<Cid>();

            while (todos.Count > 0)
            {
                var current = todos.Pop();
                // TODO: exists is never set to true!
                bool exists = false;
                using (var repo = await ipfs.Repository(cancel))
                {
                    var cid = current.Encode();
                    var pin = await repo.Pins.FindAsync(new object[] { cid }, cancel);
                    if (pin != null)
                    {
                        repo.Pins.Remove(pin);
                        await repo.SaveChangesAsync(cancel);
                    }
                }
                if (exists && recursive)
                {
                    var links = await ipfs.Object.LinksAsync(current, cancel);
                    foreach (var link in links)
                    {
                        todos.Push(link.Id);
                    }
                }
                dones.Add(current);
            }

            return dones;
        }
    }
}
