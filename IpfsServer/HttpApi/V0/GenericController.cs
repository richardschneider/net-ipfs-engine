using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Ipfs.Server.HttpApi.V0
{
    /// <summary>
    ///   Some miscellaneous methods.
    /// </summary>
    public class GenericController : IpfsController
    {
        /// <summary>
        ///   Creates a new instance of the controller.
        /// </summary>
        public GenericController(ICoreApi ipfs) : base(ipfs) { }

        /// <summary>
        ///   Information about the peer.
        /// </summary>
        /// <param name="arg">
        ///   The peer's ID or empty for the local peer.
        /// </param>
        [HttpGet, HttpPost, Route("id")]
        public async Task<PeerInfoDto> Get(string arg)
        {
            MultiHash id = null;
            if (!String.IsNullOrEmpty(arg))
                id = arg;

            var peer = await IpfsCore.Generic.IdAsync(id, Cancel);
            return new PeerInfoDto(peer);
        }

        /// <summary>
        ///   Version information on the local peer.
        /// </summary>
        [HttpGet, HttpPost, Route("version")]
        public async Task<Dictionary<string, string>> Version()
        {
            return await IpfsCore.Generic.VersionAsync(Cancel);
        }

        /// <summary>
        ///   Resolve a name.
        /// </summary>
        /// <param name="arg">
        ///   The name to resolve. Can be CID + [/path], "/ipfs/..." or
        ///   "/ipns/...".
        /// </param>
        /// <param name="recursive">
        ///   Resolve until the result is an IPFS name. Defaults to <b>false</b>.
        /// </param>
        [HttpGet(), HttpPost(), Route("resolve")]
        public async Task<PathDto> Resolve(string arg, bool recursive = false)
        {
            var path = await IpfsCore.Generic.ResolveAsync(arg, recursive, Cancel);
            return new PathDto(path);
        }

        /// <summary>
        ///  Stop the IPFS peer.
        /// </summary>
        /// <returns></returns>
        [HttpGet, HttpPost, Route("shutdown")]
        public async Task Shutdown()
        {
            await IpfsCore.Generic.ShutdownAsync();

            Program.Shutdown();
        }

    }
}
