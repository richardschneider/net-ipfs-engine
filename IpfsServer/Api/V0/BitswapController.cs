using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;

namespace Ipfs.Server.Api.V0
{

    /// <summary>
    ///  Outstanding wants.
    /// </summary>
    public class BitswapWantsDto
    {
        /// <summary>
        ///   All the links.
        /// </summary>
        public List<BitswapLinkDto> Keys;
    }

    /// <summary>
    ///   A link to a CID.
    /// </summary>
    public class BitswapLinkDto
    {
        /// <summary>
        ///   The CID.
        /// </summary>
        [JsonProperty(PropertyName = "/")]
        public string Link;
    }

    /// <summary>
    ///   Data trading module for IPFS. Its purpose is to request blocks from and 
    ///   send blocks to other peers in the network.
    /// </summary>
    /// <remarks>
    ///   Bitswap has two primary jobs (1) Attempt to acquire blocks from the network that 
    ///   have been requested by the client and (2) Judiciously(though strategically)
    ///   send blocks in its possession to other peers who want them.
    /// </remarks>
    public class BitswapController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public BitswapController(ICoreApi ipfs) : base(ipfs) { }


        /// <summary>
        ///   The blocks that are needed by a peer.
        /// </summary>
        /// <param name="arg">
        ///   A peer ID or empty for self.
        /// </param>
        [HttpGet, HttpPost, Route("bitswap/wantlist")]
        public async Task<BitswapWantsDto> Wants(string arg)
        {
            MultiHash peer = String.IsNullOrEmpty(arg) ? null : new MultiHash(arg);
            var cids = await IpfsCore.Bitswap.WantsAsync(peer, Timeout.Token);
            return new BitswapWantsDto
            {
                Keys = cids.Select(cid => new BitswapLinkDto
                {
                    Link = cid
                }).ToList()
            };
        }

        [HttpGet, HttpPost, Route("bitswap/unwant")]
        public async Task Unwants(string arg)
        {
            await IpfsCore.Bitswap.UnwantAsync(arg, Timeout.Token);
        }

    }
}
