using McMaster.Extensions.CommandLineUtils;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Ipfs.Cli
{
    [Command(Description = "Download IPFS data")]
    class GetCommand : CommandBase
    {
        [Argument(0, "ipfs-path", "The path to the IPFS data")]
        [Required]
        public string IpfsPath { get; set; }

        [Option("-o|--output", Description = "The output path for the data")]
        public string OutputBasePath { get; set; }

        [Option("-c|--compress", Description = "Create a ZIP compressed file")]
        public bool Compress { get; set; }

        Program Parent { get; set; }

        // when requested equals processed then the task is done.
        int requested = 1;
        int processed = 0;

        // ZipArchive is NOT thread safe
        ZipArchive zip;
        readonly AsyncLock ZipLock = new AsyncLock();

        class IpfsFile
        {
            public string Path;
            public IFileSystemNode Node;
        }

        ActionBlock<IpfsFile> fetch;

        async Task FetchFileOrDirectory(IpfsFile file)
        {
            if (file.Node.IsDirectory)
            {
                foreach (var link in file.Node.Links)
                {
                    var next = new IpfsFile
                    {
                        Path = Path.Combine(file.Path, link.Name),
                        Node = await Parent.CoreApi.FileSystem.ListFileAsync(link.Id)
                    };
                    ++requested;
                    fetch.Post(next);
                }
            }
            else
            {
                if (zip != null)
                {
                    await SaveToZip(file);
                }

                else
                {
                    await SaveToDisk(file);
                }
            }

            if (++processed == requested)
            {
                fetch.Complete();
            }
        }

        async Task SaveToZip(IpfsFile file)
        {
            using (var instream = await Parent.CoreApi.FileSystem.ReadFileAsync(file.Node.Id))
            using (await ZipLock.LockAsync())
            using (var entryStream = zip.CreateEntry(file.Path).Open())
            {
                await instream.CopyToAsync(entryStream);
            }
        }

        async Task SaveToDisk(IpfsFile file)
        {
            var outputPath = Path.GetFullPath(Path.Combine(OutputBasePath, file.Path));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            using (var instream = await Parent.CoreApi.FileSystem.ReadFileAsync(file.Node.Id))
            using (var outstream = File.Create(outputPath))
            {
                await instream.CopyToAsync(outstream);
            }
        }

        protected override async Task<int> OnExecute(CommandLineApplication app)
        {
            OutputBasePath = OutputBasePath ?? Path.Combine(".", IpfsPath);

            if (Compress)
            {
                var zipPath = Path.GetFullPath(OutputBasePath);
                if (!Path.HasExtension(zipPath))
                    zipPath = Path.ChangeExtension(zipPath, ".zip");
                app.Out.WriteLine($"Saving to {zipPath}");
                zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            }

            try
            {
                var options = new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 10
                };
                fetch = new ActionBlock<IpfsFile>(FetchFileOrDirectory, options);
                var first = new IpfsFile
                {
                    Path = zip == null ? "" : IpfsPath,
                    Node = await Parent.CoreApi.FileSystem.ListFileAsync(IpfsPath)
                };
                fetch.Post(first);
                await fetch.Completion;
            }
            finally
            {
                if (zip != null)
                    zip.Dispose();
            }
            return 0;
        }

    }
}
