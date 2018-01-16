using System;
using System.Collections.Generic;
using System.Text;
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
                return await ipfs.LocalPeer(cancel);
            }

            throw new NotImplementedException();
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
