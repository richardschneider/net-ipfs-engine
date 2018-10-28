using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [HelpOption("--help")]
    abstract class CommandBase
    {
        protected virtual Task<int> OnExecute(CommandLineApplication app)
        {
            app.Error.WriteLine($"The command '{app.Name}' is not implemented.");
            return Task.FromResult(1);
        }
    }
}
