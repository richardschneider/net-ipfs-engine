using Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Ipfs.Engine.UnixFileSystem;
using ProtoBuf;
using System.Linq;

namespace Ipfs.Engine.CoreApi
{
    class FileSystemApi : IFileSystemApi
    {
        static ILog log = LogManager.GetLogger(typeof(FileSystemApi));
        static byte[] emptyData = new byte[0];
        IpfsEngine ipfs;

        public FileSystemApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<IFileSystemNode> AddFileAsync(
            string path, 
            AddFileOptions options = default(AddFileOptions),
            CancellationToken cancel = default(CancellationToken))
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return await AddAsync(stream, Path.GetFileName(path), options, cancel);
            }
        }

        public Task<IFileSystemNode> AddTextAsync(
            string text,
            AddFileOptions options = default(AddFileOptions),
            CancellationToken cancel = default(CancellationToken))
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(text), false))
            {
                return AddAsync(ms, "", options, cancel);
            }
        }

        public async Task<IFileSystemNode> AddAsync(
            Stream stream, 
            string name, 
            AddFileOptions options, 
            CancellationToken cancel)
        {
            options = options ?? new AddFileOptions();

            // TODO: various options
            if (options.OnlyHash) throw new NotImplementedException("OnlyHash");
            if (options.RawLeaves) throw new NotImplementedException("RawLeaves");
            if (options.Trickle) throw new NotImplementedException("Trickle");

            var chunker = new SizeChunker();
            var nodes = await chunker.ChunkAsync(stream, options, ipfs.Block, cancel);

            // Multiple nodes for the file?
            FileSystemNode node = null;
            if (nodes.Count() == 1)
            {
                node = nodes.First();
            }
            else
            {
                // Build the DAG that contains all the file nodes.
                var links = nodes.Select(n => n.ToLink()).ToArray();
                var fileSize = (ulong)nodes.Sum(n => n.Size);
                var dm = new DataMessage
                {
                    Type = DataType.File,
                    FileSize = fileSize,
                    BlockSizes = nodes.Select(n => (ulong) n.Size).ToArray()
                };
                var pb = new MemoryStream();
                ProtoBuf.Serializer.Serialize<DataMessage>(pb, dm);
                var dag = new DagNode(pb.ToArray(), links, options.Hash);

                // Save it.
                dag.Id = await ipfs.Block.PutAsync(
                    data: dag.ToArray(),
                    multiHash: options.Hash,
                    pin: options.Pin,
                    cancel: cancel);

                node = new FileSystemNode
                {
                    Id = dag.Id,
                    Size = (long)dm.FileSize,
                    DagSize = dag.Size,
                    Links = links
                };
            }

            // Wrap in directory?
            if (options.Wrap)
            {
                var link = node.ToLink(name);
                var wlinks = new IFileSystemLink[] { link };
                return await CreateDirectoryAsync(wlinks, options, cancel);
            }

            // Return the file system node.
            node.Name = name;
            return node;
        }

        public async Task<IFileSystemNode> AddDirectoryAsync(
            string path, 
            bool recursive = true, 
            AddFileOptions options = default(AddFileOptions),
            CancellationToken cancel = default(CancellationToken))
        {
            options = options ?? new AddFileOptions();
            options.Wrap = false;

            // Add the files and sub-directories.
            path = Path.GetFullPath(path);
            var files = Directory
                .EnumerateFiles(path)
                .OrderBy(s => s)
                .Select(p => AddFileAsync(p, options, cancel));
            if (recursive)
            {
                var folders = Directory
                    .EnumerateDirectories(path)
                    .OrderBy(s => s)
                    .Select(dir => AddDirectoryAsync(dir, recursive, options, cancel));
                files = files.Union(folders);
            }
            var nodes = await Task.WhenAll(files);

            // Create the DAG with links to the created files and sub-directories
            var links = nodes
                .Select(node => node.ToLink())
                .ToArray();
            var fsn = await CreateDirectoryAsync(links, options, cancel);
            fsn.Name = Path.GetFileName(path);
            return fsn;
        }

        async Task<FileSystemNode> CreateDirectoryAsync (IEnumerable<IFileSystemLink> links, AddFileOptions options, CancellationToken cancel)
        {
            var dm = new DataMessage { Type = DataType.Directory };
            var pb = new MemoryStream();
            ProtoBuf.Serializer.Serialize<DataMessage>(pb, dm);
            var dag = new DagNode(pb.ToArray(), links, options.Hash);

            // Save it.
            var cid = await ipfs.Block.PutAsync(
                data: dag.ToArray(),
                multiHash: options.Hash,
                pin: options.Pin,
                cancel: cancel);

            return new FileSystemNode
            {
                Id = cid,
                Links = links,
                IsDirectory = true
            };
        }

        public async Task<IFileSystemNode> ListFileAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            var cid = await ipfs.ResolveIpfsPathToCidAsync(path, cancel);
            var block = await ipfs.Block.GetAsync(cid, cancel);
            var dag = new DagNode(block.DataStream);
            var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);

            // TODO: Cannot determine if a link is to a directory!
            // Maybe remove IFileSystemLink.IsDirectory
            return new FileSystemNode
            {
                Id = cid,
                Links = dag.Links
                    .Select(l => new FileSystemLink
                    {
                        Id = l.Id,
                        Name = l.Name,
                        Size = l.Size
                    })
                    .ToArray(),
                IsDirectory = dm.Type == DataType.Directory,
                Size = (long) (dm.FileSize ?? 0)
            };
        }

        public async Task<string> ReadAllTextAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            using (var data = await ReadFileAsync(path, cancel))
            using (var text = new StreamReader(data))
            {
                return await text.ReadToEndAsync();
            }
        }

        public async Task<Stream> ReadFileAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            var cid = await ipfs.ResolveIpfsPathToCidAsync(path, cancel);
            var block = await ipfs.Block.GetAsync(cid, cancel);
            var dag = new DagNode(block.DataStream);
            var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);

            if (dm.Type != DataType.File)
                throw new Exception($"'{path} is not a file.");

            return new MemoryStream(buffer: dm.Data ?? emptyData, writable: false);
        }
    }
}
