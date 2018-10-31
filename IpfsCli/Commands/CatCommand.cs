using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Show IPFS file data")]
    class CatCommand : CommandBase
    {
        [Argument(0, "ref", "The IPFS path to the data")]
        [Required]
        public string IpfsPath { get; set; }

        [Option("-o|--offset", Description = "Byte offset to begin reading from")]
        public long Offset { get; set; }

        [Option("-l|--length", Description = "Maximum number of bytes to read")]
        public long Length { get; set; }

        Program Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            using (var stream = await Parent.CoreApi.FileSystem.ReadFileAsync(IpfsPath, Offset, Length))
            {
                var stdout = Console.OpenStandardOutput();
                await stream.CopyToAsync(stdout);
            }
            return 0;
        }

    }
}
