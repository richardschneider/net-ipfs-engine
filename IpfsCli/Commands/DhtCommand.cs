using Ipfs.Engine;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Query the DHT for values or peers")]
    [Subcommand("findpeer", typeof(DhtFindPeerCommand))]
    [Subcommand("findprovs", typeof(DhtFindProvidersCommand))]
    class DhtCommand : CommandBase
    {
        public Program Parent { get; set; }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }
    }

    [Command(Description = "Find the multiaddresses associated with the peer ID")]
    class DhtFindPeerCommand : CommandBase
    {
        DhtCommand Parent { get; set; }

        [Argument(0, "peerid", "The IPFS peer ID")]
        [Required]
        public string PeerId { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;

            var peer = await Program.CoreApi.Dht.FindPeerAsync(new MultiHash(PeerId));
            return Program.Output(app, peer, (data, writer) =>
            {
                foreach (var a in peer.Addresses)
                {
                    writer.WriteLine(a.ToString());
                }
            });
        }
    }

    [Command(Description = "Find peers that can provide a specific value, given a key")]
    class DhtFindProvidersCommand : CommandBase
    {
        DhtCommand Parent { get; set; }

        [Argument(0, "key", "The multihash key")]
        [Required]
        public string Key { get; set; }

        [Option("-n|--num-providers", Description = "The number of providers to find")]
        public int Limit { get; set; } = 20;

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;

            var peers = await Program.CoreApi.Dht.FindProvidersAsync(new MultiHash(Key), Limit);
            return Program.Output(app, peers, (data, writer) =>
            {
                foreach (var peer in peers)
                {
                    writer.WriteLine(peer.Id.ToString());
                }
            });
        }
    }
}
