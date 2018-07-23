using Ipfs;
using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Ipfs.Server.Api.V0
{
    [Route("api/v0/dns")]
    [Produces("application/json")]
    public class DnsController : Controller
    {
        ICoreApi ipfs;

        public DnsController(ICoreApi ipfs)
        {
            this.ipfs = ipfs;
        }

        [HttpGet, HttpPost]
        public async Task<PathDto> Get(string arg, bool recursive = false)
        {
            var path = await ipfs.Dns.ResolveAsync(arg, recursive);
            return new PathDto(path);
        }

    }
}
