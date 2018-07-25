using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Ipfs.Server.Api.V0
{
    /// <summary>
    ///   A list of pins.
    /// </summary>
    public class PinsDto
    {
        /// <summary>
        ///   The CIDs.
        /// </summary>
        public IEnumerable<string> Pins;
    }

    /// <summary>
    ///   Detailed information on a pin.
    /// </summary>
    public class PinDetailDto
    {
        /// <summary>
        ///   "recursive", "indirect", ...
        /// </summary>
        public string Type = "unknown";
    }

    /// <summary>
    ///   A map of pins.
    /// </summary>
    public class PinDetailsDto
    {
        /// <summary>
        ///   The pins.
        /// </summary>
        public Dictionary<string, PinDetailDto> Keys;
    }

    /// <summary>
    ///    Manage pinned objects (locally stored and permanent).
    /// </summary>
    public class PinController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public PinController(ICoreApi ipfs) : base(ipfs) { }

        /// <summary>
        ///   List the pins.
        /// </summary>
        [HttpGet, HttpPost, Route("pin/ls")]
        public async Task<PinDetailsDto> List()
        {
            var cids = await IpfsCore.Pin.ListAsync(Timeout.Token);
            return new PinDetailsDto
            {
                Keys = cids.ToDictionary(cid => cid.Encode(), cid => new PinDetailDto())
            };
        }

        /// <summary>
        ///   Pin the content.
        /// </summary>
        /// <param name="arg">
        ///   The CID of the content.
        /// </param>
        /// <param name="recursive">
        ///   Recursively pin links of the content.
        /// </param>
        [HttpGet, HttpPost, Route("pin/add")]
        public async Task<PinsDto> Add(
            string arg,
            bool recursive = true)
        {
            var cids = await IpfsCore.Pin.AddAsync(arg, recursive, Timeout.Token);
            return new PinsDto
            {
                Pins = cids.Select(cid => cid.Encode())
            };
        }


        /// <summary>
        ///   Remove a pin.
        /// </summary>
        /// <param name="arg">
        ///   The CID of the content.
        /// </param>
        /// <param name="recursive">
        ///   Recursively unpin links of the content.
        /// </param>
        [HttpGet, HttpPost, Route("pin/rm")]
        public async Task<PinsDto> Remove(
            string arg,
            bool recursive = true)
        {
            var cids = await IpfsCore.Pin.RemoveAsync(arg, recursive, Timeout.Token);
            return new PinsDto
            {
                Pins = cids.Select(cid => cid.Encode())
            };
        }
    }
}
