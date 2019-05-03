using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using System.Reflection;

namespace Ipfs.Engine.CoreApi
{
    class GenericApi : IGenericApi
    {
        IpfsEngine ipfs;

        public GenericApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<Peer> IdAsync(MultiHash peer = null, CancellationToken cancel = default(CancellationToken))
        {
            if (peer == null)
            {
                return await ipfs.LocalPeer.ConfigureAwait(false);
            }

            return await ipfs.Dht.FindPeerAsync(peer, cancel).ConfigureAwait(false);
        }

        public async Task<string> ResolveAsync(string name, bool recursive = false, CancellationToken cancel = default(CancellationToken))
        {
            var path = name;
            if (path.StartsWith("/ipns/"))
            {
                path = await ipfs.Name.ResolveAsync(path, recursive, false, cancel).ConfigureAwait(false);
                if (!recursive)
                    return path;
            }

            if (path.StartsWith("/ipfs/")) {
                path = path.Remove(0, 6);
            }

            var parts = path.Split('/').Where(p => p.Length > 0).ToArray();
            if (parts.Length == 0)
                throw new ArgumentException($"Cannot resolve '{name}'.");

            var id = Cid.Decode(parts[0]);
            foreach (var child in parts.Skip(1))
            {
                var container = await ipfs.Object.GetAsync(id, cancel).ConfigureAwait(false);
                var link = container.Links.FirstOrDefault(l => l.Name == child);
                if (link == null)
                    throw new ArgumentException($"Cannot resolve '{name}'.");
                id = link.Id;
            }

            return "/ipfs/" + id.Encode();
        }

        public Task ShutdownAsync()
        {
            return ipfs.StopAsync();
        }

        public Task<Dictionary<string, string>> VersionAsync(CancellationToken cancel = default(CancellationToken))
        {
            var version = typeof(GenericApi).GetTypeInfo().Assembly.GetName().Version;
            return Task.FromResult(new Dictionary<string, string>
            {
                { "Version", $"{version.Major}.{version.Minor}.{version.Revision}" },
                { "Repo", "0" }
            });
        }
    }
}
