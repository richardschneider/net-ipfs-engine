using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Resolve DNS link")]
    class DnsCommand : CommandBase
    {
        [Argument(0, "domain-name", "The DNS domain name")]
        [Required]
        public string Name { get; set; }

        [Option("-r|--recursive", Description = "Resolve until the result is not a DNS link")]
        public bool Recursive { get; set; }

        Program Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var result = await Parent.CoreApi.Dns.ResolveAsync(Name, Recursive);
            app.Out.Write(result);
            return 0;
        }
    }
}
