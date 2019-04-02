using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;

namespace Ipfs.Engine.CoreApi
{
    class BootstrapApi : IBootstrapApi
    {
        IpfsEngine ipfs;

        // From https://github.com/libp2p/go-libp2p-daemon/blob/master/bootstrap.go#L14
        // TODO: Missing the /dnsaddr/... addresses
        static MultiAddress[] defaults = new MultiAddress[]
        {
            "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ",            // mars.i.ipfs.io
	        "/ip4/104.236.179.241/tcp/4001/ipfs/QmSoLPppuBtQSGwKDZT2M73ULpjvfd3aZ6ha4oFGL1KrGM",           // pluto.i.ipfs.io
	        "/ip4/128.199.219.111/tcp/4001/ipfs/QmSoLSafTMBsPKadTEgaXctDQVcqN88CNLHXMkTNwMKPnu",           // saturn.i.ipfs.io
	        "/ip4/104.236.76.40/tcp/4001/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64",             // venus.i.ipfs.io
	        "/ip4/178.62.158.247/tcp/4001/ipfs/QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd",            // earth.i.ipfs.io
	        "/ip6/2604:a880:1:20::203:d001/tcp/4001/ipfs/QmSoLPppuBtQSGwKDZT2M73ULpjvfd3aZ6ha4oFGL1KrGM",  // pluto.i.ipfs.io
	        "/ip6/2400:6180:0:d0::151:6001/tcp/4001/ipfs/QmSoLSafTMBsPKadTEgaXctDQVcqN88CNLHXMkTNwMKPnu",  // saturn.i.ipfs.io
	        "/ip6/2604:a880:800:10::4a:5001/tcp/4001/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64", // venus.i.ipfs.io
	        "/ip6/2a03:b0c0:0:1010::23:1001/tcp/4001/ipfs/QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd", // earth.i.ipfs.io
        };

        public BootstrapApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<MultiAddress> AddAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            // Throw if missing peer ID
            var _ = address.PeerId;

            var addrs = (await ListAsync(cancel)).ToList();
            if (addrs.Any(a => a == address))
                return address;

            addrs.Add(address);
            var strings = addrs.Select(a => a.ToString());
            await ipfs.Config.SetAsync("Bootstrap", JToken.FromObject(strings), cancel);
            return address;
        }

        public async Task<IEnumerable<MultiAddress>> AddDefaultsAsync(CancellationToken cancel = default(CancellationToken))
        {
            foreach (var a in defaults)
            {
                await AddAsync(a, cancel);
            }

            return defaults;
        }

        public async Task<IEnumerable<MultiAddress>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            if (ipfs.Options.Discovery.BootstrapPeers != null)
            {
                return ipfs.Options.Discovery.BootstrapPeers;
            }

            try
            {
                var json = await ipfs.Config.GetAsync("Bootstrap", cancel);
                if (json == null)
                    return new MultiAddress[0];

                return json
                    .Select(a => MultiAddress.TryCreate((string)a))
                    .Where(a => a != null);
            }
            catch (KeyNotFoundException)
            {
                var strings = defaults.Select(a => a.ToString());
                await ipfs.Config.SetAsync("Bootstrap", JToken.FromObject(strings), cancel);
                return defaults;
            }
        }

        public async Task RemoveAllAsync(CancellationToken cancel = default(CancellationToken))
        {
            await ipfs.Config.SetAsync("Bootstrap", JToken.FromObject(new string[0]), cancel);
        }

        public async Task<MultiAddress> RemoveAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            var addrs = (await ListAsync(cancel)).ToList();
            if (!addrs.Any(a => a == address))
                return address;

            addrs.Remove(address);
            var strings = addrs.Select(a => a.ToString());
            await ipfs.Config.SetAsync("Bootstrap", JToken.FromObject(strings), cancel);
            return address;
        }
    }
}
