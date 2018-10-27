using Ipfs.CoreApi;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Cli
{
    [Command(Description = "Add a file to IPFS")]
    class AddCommand : CommandBase
    {
        static AddFileOptions DefaultOptions = new AddFileOptions();

        [Argument(0, "path", "The path to a file to be added to ipfs")]
        [FileOrDirectoryExists]
        [Required]
        public string FilePath { get; set; }

        [Option("--chunk-size", Description = "The maximum number of bytes in a block")]
        public int ChunkSize { get; set; } = DefaultOptions.ChunkSize;

        [Option("--encoding", Description = "CID encoding algorithm")]
        public string Encoding { get; set; } = DefaultOptions.Encoding;

        [Option("--hash", Description = "The hashing algorithm")]
        public string Hash { get; set; } = DefaultOptions.Hash;

        [Option("-n|--only-hash", Description = "Only chunk and hash - do not write to disk")]
        public bool OnlyHash { get; set; } = DefaultOptions.OnlyHash;

        [Option("--pin", Description = "Pin when adding")]
        public bool Pin { get; set; } = DefaultOptions.Pin;

        [Option("--raw-leaves", Description = "Raw data for leaf nodes")]
        public bool RawLeaves { get; set; } = DefaultOptions.RawLeaves;

        [Option("-t|--trickle", Description = "Use trickle dag format")]
        public bool Trickle { get; set; } = DefaultOptions.Trickle;

        [Option("-w|--wrap", Description = "Wrap file in a directory")]
        public bool Wrap { get; set; } = DefaultOptions.Wrap;

        Program Parent { get; set; }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            AddFileOptions options = new AddFileOptions
            {
                ChunkSize = ChunkSize,
                Encoding = Encoding,
                Hash = Hash,
                OnlyHash = OnlyHash,
                Pin = Pin,
                RawLeaves = RawLeaves,
                Trickle = Trickle,
                Wrap = Wrap
            };
            var node = await Parent.CoreApi.FileSystem.AddFileAsync(FilePath, options);
            app.Out.WriteLine($"{node.Id.Encode()} added");
            return 0;
        }

    }
}
