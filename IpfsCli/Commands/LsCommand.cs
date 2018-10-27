using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "List links")]
    class LsCommand : CommandBase
    {
        [Argument(0, "ipfs-path", "The path to an IPFS object")]
        [Required]
        public string IpfsPath { get; set; }

        Program Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            var node = await Parent.CoreApi.FileSystem.ListFileAsync(IpfsPath);
            foreach (var link in node.Links)
            {
                app.Out.WriteLine($"{link.Id.Encode()} {link.Size} {link.Name}");
            }
            return 0;
        }

    }
}
