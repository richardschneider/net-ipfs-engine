using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Ipfs.Server.Api.V0
{
    public class DnsController : IpfsController
    {
        public DnsController(ICoreApi ipfs) : base(ipfs) { }

        [HttpGet, HttpPost, Route("dns")]
        public async Task<PathDto> Get(string arg, bool recursive = false)
        {
            var path = await IpfsCore.Dns.ResolveAsync(arg, recursive);
            return new PathDto(path);
        }

    }
}
