using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Concurrent;
using PeerTalk;
using Common.Logging;

namespace Ipfs.Engine.CoreApi
{
    class SwarmApi : ISwarmApi
    {
        static ILog log = LogManager.GetLogger(typeof(SwarmApi));
        IpfsEngine ipfs;

        static MultiAddress[] defaultFilters = new MultiAddress[]
        {
        };

        public SwarmApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<MultiAddress> AddAddressFilterAsync(MultiAddress address, bool persist = false, CancellationToken cancel = default(CancellationToken))
        {
            var addrs = (await ListAddressFiltersAsync(persist, cancel).ConfigureAwait(false)).ToList();
            if (addrs.Any(a => a == address))
                return address;

            addrs.Add(address);
            var strings = addrs.Select(a => a.ToString());
            await ipfs.Config.SetAsync("Swarm.AddrFilters", JToken.FromObject(strings), cancel).ConfigureAwait(false);

            (await ipfs.SwarmService.ConfigureAwait(false)).WhiteList.Add(address);

            return address;
        }

        public async Task<IEnumerable<Peer>> AddressesAsync(CancellationToken cancel = default(CancellationToken))
        {
            var swarm = await ipfs.SwarmService.ConfigureAwait(false);
            return swarm.KnownPeers;
        }

        public async Task ConnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            var swarm = await ipfs.SwarmService.ConfigureAwait(false);
            log.Debug($"Connecting to {address}");
            var peer = await swarm.ConnectAsync(address, cancel).ConfigureAwait(false);
            log.Debug($"Connected to {peer.ConnectedAddress}");
        }

        public async Task DisconnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            var swarm = await ipfs.SwarmService.ConfigureAwait(false);
            await swarm.DisconnectAsync(address, cancel).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MultiAddress>> ListAddressFiltersAsync(bool persist = false, CancellationToken cancel = default(CancellationToken))
        {
            try
            {
                var json = await ipfs.Config.GetAsync("Swarm.AddrFilters", cancel).ConfigureAwait(false);
                if (json == null)
                    return new MultiAddress[0];

                return json
                    .Select(a => MultiAddress.TryCreate((string)a))
                    .Where(a => a != null);
            }
            catch (KeyNotFoundException)
            {
                var strings = defaultFilters.Select(a => a.ToString());
                await ipfs.Config.SetAsync("Swarm.AddrFilters", JToken.FromObject(strings), cancel).ConfigureAwait(false);
                return defaultFilters;
            }
        }

        public async Task<IEnumerable<Peer>> PeersAsync(CancellationToken cancel = default(CancellationToken))
        {
            var swarm = await ipfs.SwarmService.ConfigureAwait(false);
            return swarm.KnownPeers.Where(p => p.ConnectedAddress != null);
        }

        public async Task<MultiAddress> RemoveAddressFilterAsync(MultiAddress address, bool persist = false, CancellationToken cancel = default(CancellationToken))
        {
            var addrs = (await ListAddressFiltersAsync(persist, cancel).ConfigureAwait(false)).ToList();
            if (!addrs.Any(a => a == address))
                return null;

            addrs.Remove(address);
            var strings = addrs.Select(a => a.ToString());
            await ipfs.Config.SetAsync("Swarm.AddrFilters", JToken.FromObject(strings), cancel).ConfigureAwait(false);

            var bag = new WhiteList<MultiAddress>();
            foreach (var a in addrs)
            {
                bag.Add(a);
            }
            (await ipfs.SwarmService.ConfigureAwait(false)).WhiteList = bag;

            return address;
        }
    }
}
