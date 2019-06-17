using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Ipfs.Server.HttpApi.V0
{
    /// <summary>
    ///  Statistics for bitswap.
    /// </summary>
    public class StatsBitswapDto
    {
        /// <summary>
        ///   TODO: Unknown.
        /// </summary>
        public int ProvideBufLen;

        /// <summary>
        ///   The content IDs that are wanted.
        /// </summary>
        public IEnumerable<BitswapLinkDto> Wantlist;

        /// <summary>
        ///   The known peers.
        /// </summary>
        public IEnumerable<string> Peers;

        /// <summary>
        ///   The number of blocks sent by other peers.
        /// </summary>
        public ulong BlocksReceived;

        /// <summary>
        ///   The number of bytes sent by other peers.
        /// </summary>
        public ulong DataReceived;

        /// <summary>
        ///   The number of blocks sent to other peers.
        /// </summary>
        public ulong BlocksSent;

        /// <summary>
        ///   The number of bytes sent to other peers.
        /// </summary>
        public ulong DataSent;

        /// <summary>
        ///   The number of duplicate blocks sent by other peers.
        /// </summary>
        /// <remarks>
        ///   A duplicate block is a block that is already stored in the
        ///   local repository.
        /// </remarks>
        public ulong DupBlksReceived;

        /// <summary>
        ///   The number of duplicate bytes sent by other peers.
        /// </summary>
        /// <remarks>
        ///   A duplicate block is a block that is already stored in the
        ///   local repository.
        /// </remarks>
        public ulong DupDataReceived;
    }

    /// <summary>
    ///    Get the statistics on various IPFS components.
    /// </summary>
    public class StatsController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public StatsController(ICoreApi ipfs) : base(ipfs) { }

        /// <summary>
        ///   Get bandwidth information.
        /// </summary>
        [HttpGet, HttpPost, Route("stats/bw")]
        public Task<BandwidthData> Bandwidth()
        {
            Response.Headers.Add("X-Chunked-Output", "1");
            return IpfsCore.Stats.BandwidthAsync(Cancel);
        }

        /// <summary>
        ///   Get bitswap information.
        /// </summary>
        [HttpGet, HttpPost, Route("stats/bitswap"), Route("bitswap/stat")]
        public async Task<StatsBitswapDto> Bitswap()
        {
            Response.Headers.Add("X-Chunked-Output", "1");
            var data =  await IpfsCore.Stats.BitswapAsync(Cancel);
            return new StatsBitswapDto
            {
                BlocksReceived = data.BlocksReceived,
                BlocksSent = data.BlocksSent,
                DataReceived = data.DataReceived,
                DataSent = data.DataSent,
                DupBlksReceived = data.DupBlksReceived,
                DupDataReceived = data.DupDataReceived,
                ProvideBufLen = data.ProvideBufLen,
                Peers = data.Peers.Select(peer => peer.ToString()),
                Wantlist = data.Wantlist.Select(cid => new BitswapLinkDto { Link = cid })
            };
        }

        /// <summary>
        ///   Get repository information.
        /// </summary>
        [HttpGet, HttpPost, Route("stats/repo")]
        public Task<RepositoryData> Repo()
        {
            Response.Headers.Add("X-Chunked-Output", "1");
            return IpfsCore.Stats.RepositoryAsync(Cancel);
        }
    }
}
