using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ipfs.Cli
{
    [Command(Description = "Show IPFS data [WIP]")]
    class CatCommand : CommandBase
    {
        [Argument(0, "ref", "The IPFS path to the data")]
        public string Path { get; set; }

        [Option("-o|--offset", Description = "Byte offset to begin reading from")]
        public long Offset { get; set; }

        [Option("-l|--length", Description = "Maximum number of bytes to read")]
        public long Length { get; set; }

    }
}
