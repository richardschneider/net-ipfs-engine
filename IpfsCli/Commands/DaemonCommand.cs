using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Start a long running IPFS deamon")]
    class DaemonCommand : CommandBase // TODO
    {
        Program Parent { get; set; }

        protected override Task<int> OnExecute(CommandLineApplication app)
        {
            Ipfs.Server.Program.Main(new string[0]);
            return Task.FromResult(0);
        }
    }
}
