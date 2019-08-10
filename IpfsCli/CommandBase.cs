using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    
    abstract class CommandBase
    {
        protected virtual Task<int> OnExecute(CommandLineApplication app)
        {
            app.Error.WriteLine($"The command '{app.Name}' is not implemented.");
            return Task.FromResult(1);
        }
    }
}
