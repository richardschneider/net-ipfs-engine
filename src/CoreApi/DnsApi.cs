using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using System.Linq;

namespace Ipfs.Engine.CoreApi
{
    class DnsApi : IDnsApi
    {
        IpfsEngine ipfs;

        public DnsApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<string> ResolveAsync(string name, bool recursive = false, CancellationToken cancel = default(CancellationToken))
        {
            var response = await DnsClient.QueryAsync(name, DnsType.TXT, cancel);
            var link = response.Answers
                .OfType<TXTRecord>()
                .SelectMany(txt => txt.Strings)
                .Where(s => s.StartsWith("dnslink="))
                .Select(s => s.Substring(8))
                .First();

            if (recursive && !link.StartsWith("/ipfs/"))
                throw new NotImplementedException("Following DNS recursive link.");

            return link;
        }
    }
}
