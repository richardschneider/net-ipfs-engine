using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Manage IPNS names")]
    [Subcommand("resolve", typeof(NameResolveCommand))]
    class NameCommand : CommandBase
    {
        public Program Parent { get; set; }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }
    }

    [Command(Description = "Resolve a name")]
    class NameResolveCommand : CommandBase
    {
        NameCommand Parent { get; set; }

        [Argument(0, "name", "A key or a DNS name")]
        [Required]
        public string Name { get; set; }

        [Option("-r|--recursive", Description = "Resolve until the result is an IPFS name")]
        public bool Recursive { get; set; }

        [Option("-n|--nocache", Description = "Do not use cached entries")]
        public bool NoCache { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;

            var resolved = await Program.CoreApi.Name.ResolveAsync(Name, Recursive, NoCache);
            app.Out.Write(resolved);
            return 0;
        }
    }

}
