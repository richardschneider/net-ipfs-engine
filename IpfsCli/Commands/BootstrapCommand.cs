using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Manage bootstrap peers")]
    [Subcommand("list", typeof(BootstrapListCommand))]
    [Subcommand("rm", typeof(BootstrapRemoveCommand))]
    [Subcommand("add", typeof(BootstrapAddCommand))]
    class BootstrapCommand : CommandBase
    {
        public Program Parent { get; set; }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }
    }

    [Command(Description = "List the bootstrap peers")]
    class BootstrapListCommand : CommandBase
    {
        BootstrapCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            var peers = await Program.CoreApi.Bootstrap.ListAsync();
            return Program.Output(app, peers, (data, writer) =>
            {
                foreach (var addresss in data)
                {
                    writer.WriteLine(addresss);
                }
            });
        }
    }

    [Command(Description = "Add the bootstrap peer")]
    [Subcommand("default", typeof(BootstrapAddDefaultCommand))]
    class BootstrapAddCommand : CommandBase
    {
        [Argument(0, "addr", "A multiaddress to the peer")]
        public string Address { get; set; }

        public BootstrapCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            await Program.CoreApi.Bootstrap.AddAsync(Address);
            return 0;
        }
    }

    [Command(Description = "Add the default bootstrap peers")]
    class BootstrapAddDefaultCommand : CommandBase
    {
        BootstrapAddCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent.Parent;
            var peers = await Program.CoreApi.Bootstrap.AddDefaultsAsync();
            return Program.Output(app, peers, (data, writer) =>
            {
                foreach (var a in data)
                {
                    writer.WriteLine(a);
                }
            });
        }
    }

    [Command(Description = "Remove the bootstrap peer")]
    [Subcommand("all", typeof(BootstrapRemoveAllCommand))]
    class BootstrapRemoveCommand : CommandBase
    {
        [Argument(0, "addr", "A multiaddress to the peer")]
        public string Address { get; set; }

        public BootstrapCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent;
            await Program.CoreApi.Bootstrap.RemoveAsync(Address);
            return 0;
        }
    }

    [Command(Description = "Remove all the bootstrap peers")]
    class BootstrapRemoveAllCommand : CommandBase
    {
        BootstrapRemoveCommand Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var Program = Parent.Parent.Parent;
            await Program.CoreApi.Bootstrap.RemoveAllAsync();
            return 0;
        }
    }
}
