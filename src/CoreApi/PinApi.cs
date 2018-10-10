using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Microsoft.EntityFrameworkCore;

namespace Ipfs.Engine.CoreApi
{
    class Pin
    {
        public static Pin Default = new Pin();
    }

    class PinApi : IPinApi
    {
        IpfsEngine ipfs;
        FileStore<Cid, Pin> store;

        public PinApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        FileStore<Cid, Pin> Store
        {
            get
            {
                if (store == null)
                {
                    var folder = Path.Combine(ipfs.Options.Repository.Folder, "pins");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    // TODO: Need cid.Encode("base32")
                    store = new FileStore<Cid, Pin>
                    {
                        Folder = folder,
                        NameToKey = (cid) => cid.Encode(),
                        KeyToName = (key) => Cid.Decode(key),
                        Serialize = (stream, cid, block, cancel) => Task.CompletedTask,
                        Deserialize = (stream, cid, cancel) => Task.FromResult(Pin.Default)
                    };
                }
                return store;
            }
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
                await Store.PutAsync(current, Pin.Default);

                // Make sure that the content is stored locally.
                await ipfs.Block.GetAsync(current, cancel);

                // Recursively pin the links?
                if (recursive && current.ContentType == "dag-pb")
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

        public Task<IEnumerable<Cid>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            var cids = Store.Names.ToArray();
            return Task.FromResult((IEnumerable<Cid>)cids);
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
                await Store.RemoveAsync(current, cancel);
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
