using Ipfs.CoreApi;
using PeerTalk; // TODO: need MultiAddress.WithOutPeer (should be in IPFS code)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Ipfs.Server.HttpApi.V0
{
    /// <summary>
    ///   Information from the Distributed Hash Table.
    /// </summary>
    public class DhtPeerDto
    {
        public string ID;
        public int Type; // TODO: what is the type?
        public IEnumerable<DhtPeerResponseDto> Responses;
        public string Extra = string.Empty;
    }

    public class DhtPeerResponseDto
    {
        public string ID;
        public IEnumerable<String> Addrs;
    }

    /// <summary>
    ///   Distributed Hash Table.
    /// </summary>
    /// <remarks>
    ///   The DHT is a place to store, not the value, but pointers to peers who have 
    ///   the actual value.
    /// </remarks>
    public class DhtController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public DhtController(ICoreApi ipfs) : base(ipfs) { }

        /// <summary>
        ///   Query the DHT for all of the multiaddresses associated with a Peer ID.
        /// </summary>
        /// <param name="arg">
        ///   The peer ID to find.
        /// </param>
        /// <returns>
        ///   Information about the peer.
        /// </returns>
        [HttpGet, HttpPost, Route("dht/findpeer")]
        public async Task<DhtPeerDto> FindPeer(string arg)
        {
            var peer = await IpfsCore.Dht.FindPeerAsync(arg, Cancel);
            return new DhtPeerDto
            {
                ID = peer.Id.ToBase58(),
                Responses = new DhtPeerResponseDto[]
                {
                    new DhtPeerResponseDto
                    {
                        ID = peer.Id.ToBase58(),
                        Addrs = peer.Addresses.Select(a => a.WithoutPeerId().ToString())
                    }
                }
            };
        }

        /// <summary>
        ///  Find peers in the DHT that can provide a specific value, given a key.
        /// </summary>
        /// <param name="arg">
        ///   The CID key,
        /// </param>
        /// <param name="limit">
        ///   The maximum number of providers to find.
        /// </param>
        /// <returns>
        ///   Information about the peer providers.
        /// </returns>
        [HttpGet, HttpPost, Route("dht/findprovs")]
        public async Task<IEnumerable<DhtPeerDto>> FindProviders(
            string arg,
            [ModelBinder(Name = "num-providers")] int limit = 20
            )
        {
            var peers = await IpfsCore.Dht.FindProvidersAsync(arg, limit, Cancel);
            return peers.Select(peer => new DhtPeerDto
            {
                ID = peer.Id.ToBase58(), // TODO: should be the peer ID that answered the query
                Responses = new DhtPeerResponseDto[]
                {
                    new DhtPeerResponseDto
                    {
                        ID = peer.Id.ToBase58(),
                        Addrs = peer.Addresses.Select(a => a.WithoutPeerId().ToString())
                    }
                }
            });
        }

    }
}
