using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Show version information")]
    class VersionCommand : CommandBase
    {
        public Program Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var info = await Parent.CoreApi.Generic.VersionAsync();
            return Parent.Output(app, info, null);
        }
    }
}
