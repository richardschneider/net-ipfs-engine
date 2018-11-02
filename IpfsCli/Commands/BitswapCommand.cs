using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Manage swapped blocks")]
    [Subcommand("wantlist", typeof(BitswapWantListCommand))]
    [Subcommand("unwant", typeof(BitswapUnwantCommand))]
    class BitswapCommand : CommandBase
    {
        public Program Parent { get; set; }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }
    }

    [Command(Description = "Show blocks currently on the wantlist")]
    class BitswapWantListCommand : CommandBase
    {
        BitswapCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var cids = await Program.CoreApi.Bitswap.WantsAsync();
            return Program.Output(app, cids, (data, writer) =>
            {
                foreach (var cid in data)
                {
                    writer.WriteLine(cid.Encode());
                }
            });
        }
    }

    [Command(Description = "Remove a block from the wantlist")]
    class BitswapUnwantCommand : CommandBase
    {
        [Argument(0, "cid", "The content ID of the block")]
        [Required]
        public string Cid { get; set; }

        BitswapCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            await Program.CoreApi.Bitswap.UnwantAsync(Cid);
            return 0;
        }
    }

}
