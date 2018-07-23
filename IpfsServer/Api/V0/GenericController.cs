using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Ipfs.Server.Api.V0
{
    public class GenericController : IpfsController
    {
        /// <summary>
        ///   Creates a new instance of the controller.
        /// </summary>
        public GenericController(ICoreApi ipfs) : base(ipfs) { }

        [HttpGet, HttpPost, Route("id")]
        public async Task<PeerDto> Get()
        {
            var peer = await IpfsCore.Generic.IdAsync(null, Timeout.Token);
            return new PeerDto(peer);
        }

        [HttpGet, HttpPost, Route("version")]
        public async Task<Dictionary<string, string>> Version()
        {
            return await IpfsCore.Generic.VersionAsync(Timeout.Token);
        }

        [HttpGet(), HttpPost(), Route("resolve")]
        public async Task<PathDto> Resolve(string arg, bool recursive = false)
        {
            var path = await IpfsCore.Generic.ResolveAsync(arg, recursive, Timeout.Token);
            return new PathDto(path);
        }

        [HttpGet, HttpPost, Route("shutdown")]
        public async Task Shutdown()
        {
            await IpfsCore.Generic.ShutdownAsync();

            // TODO: return a response then shutdown the server
            Program.Shutdown();
        }

    }
}
