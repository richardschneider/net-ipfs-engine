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
using Newtonsoft.Json.Linq;
using System.IO;

namespace Ipfs.Server.HttpApi.V0
{
    /// <summary>
    ///   Addresses for peers.
    /// </summary>
    public class AddrsDto
    {
        /// <summary>
        ///   Addresses for peers.
        /// </summary>
        public Dictionary<string, List<string>> Addrs = new Dictionary<string, List<string>>();
    }

    /// <summary>
    ///   Information on a peer.
    /// </summary>
    public class ConnectedPeerDto
    {
        /// <summary>
        ///  The unique ID of the peer.
        /// </summary>
        public string Peer;

        /// <summary>
        ///   The connected address.
        /// </summary>
        public string Addr;

        /// <summary>
        ///   Avg time to the peer.
        /// </summary>
        public string Latency;

        /// <summary>
        ///   Creates a new peer info.
        /// </summary>
        public ConnectedPeerDto(Peer peer)
        {
            Peer = peer.Id.ToString();
            Addr = peer.ConnectedAddress?.WithoutPeerId().ToString();
            Latency = peer.Latency == null ? string.Empty : Duration.Stringify(peer.Latency.Value, string.Empty);
        }
    }

    /// <summary>
    ///   Information on a peer.
    /// </summary>
    public class ConnectedPeersDto
    {
        /// <summary>
        ///   The connected peers.
        /// </summary>
        public IEnumerable<ConnectedPeerDto> Peers;
    }

    /// <summary>
    ///   A list of filters.
    /// </summary>
    public class FiltersDto
    {
        /// <summary>
        ///   A list of multiaddresses.
        /// </summary>
        public string[] Strings;
    }

    /// <summary>
    ///   TODO
    /// </summary>
    public class SwarmController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public SwarmController(ICoreApi ipfs) : base(ipfs) { }

        /// <summary>
        ///   Peer addresses.
        /// </summary>
        [HttpGet, HttpPost, Route("swarm/addrs")]
        public async Task<AddrsDto> PeerAddresses()
        {
            var peers = await IpfsCore.Swarm.AddressesAsync(Cancel);
            var dto = new AddrsDto();
            foreach (var peer in peers)
            {
                dto.Addrs[peer.Id.ToString()] = peer.Addresses
                    .Select(a => a.ToString())
                    .ToList();
            }
            return dto;
        }

        /// <summary>
        ///   Connected peers.
        /// </summary>
        [HttpGet, HttpPost, Route("swarm/peers")]
        public async Task<ConnectedPeersDto> ConnectedPeers()
        {
            var peers = await IpfsCore.Swarm.PeersAsync(Cancel);
            return new ConnectedPeersDto
            {
                Peers = peers.Select(peer => new ConnectedPeerDto(peer)).ToArray()
            };
        }

        /// <summary>
        ///   List the address filters.
        /// </summary>
        [HttpGet, HttpPost, Route("swarm/filters")]
        public async Task<FiltersDto> ListFilters()
        {
            var filters = await IpfsCore.Swarm.ListAddressFiltersAsync(persist: false, cancel: Cancel);
            return new FiltersDto
            {
                Strings = filters.Select(f => f.ToString()).ToArray()
            };
        }

        /// <summary>
        ///   Add an address filter.
        /// </summary>
        /// <param name="arg">
        ///   A multiaddress.
        /// </param>
        [HttpGet, HttpPost, Route("swarm/filters/add")]
        public async Task<FiltersDto> AddFilter(string arg)
        {
            var filter = await IpfsCore.Swarm.AddAddressFilterAsync(arg, persist: false, cancel: Cancel);
            return new FiltersDto
            {
                Strings = filter == null ? new string[0] : new [] { filter.ToString() }
            };
        }

        /// <summary>
        ///   Remove an address filter.
        /// </summary>
        /// <param name="arg">
        ///   A multiaddress.
        /// </param>
        [HttpGet, HttpPost, Route("swarm/filters/rm")]
        public async Task<FiltersDto> RemoveFilter(string arg)
        {
            var filter = await IpfsCore.Swarm.RemoveAddressFilterAsync(arg, persist: false, cancel: Cancel);
            return new FiltersDto
            {
                Strings = filter == null ? new string[0] : new[] { filter.ToString() }
            };
        }

        /// <summary>
        ///   Connect to a peer.
        /// </summary>
        /// <param name="arg">
        ///   The multiaddress of the peer.
        /// </param>
        [HttpGet, HttpPost, Route("swarm/connect")]
        public Task Connect(string arg)
        {
            return IpfsCore.Swarm.ConnectAsync(arg, Cancel);
        }

        /// <summary>
        ///   Disconnect from a peer.
        /// </summary>
        /// <param name="arg">
        ///   The multiaddress of the peer.
        /// </param>
        [HttpGet, HttpPost, Route("swarm/disconnect")]
        public Task Disconnect(string arg)
        {
            return IpfsCore.Swarm.DisconnectAsync(arg, Cancel);
        }
    }
}


