using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Resolve any type of name")]
    class ResolveCommand : CommandBase
    {
        [Argument(0, "name", "The IPFS/IPNS/... name")]
        [Required]
        public string Name { get; set; }

        [Option("-r|--recursive", Description = "Resolve until the result is an IPFS name")]
        public bool Recursive { get; set; }

        Program Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var result = await Parent.CoreApi.Generic.ResolveAsync(Name, Recursive);
            app.Out.WriteLine(result);
            return 0;
        }

    }
}
