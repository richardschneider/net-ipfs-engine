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
        IpfsEngine ipfs;

        public FileSystemApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<IFileSystemNode> AddAsync(
            Stream stream, 
            string name = "", 
            AddFileOptions options = default(AddFileOptions),
            CancellationToken cancel = default(CancellationToken))
        {
            // TODO: If stream is seekable we can use .Length
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms, 8 * 1024);
                return await AddAsync(ms.ToArray(), name, options, cancel);
            }
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
            return AddAsync(Encoding.UTF8.GetBytes(text), "", options, cancel);
        }

        public async Task<IFileSystemNode> AddAsync(
            byte[] data, 
            string name, 
            AddFileOptions options, 
            CancellationToken cancel)
        {
            options = options ?? new AddFileOptions();

            // TODO: various options
            if (options.OnlyHash) throw new NotImplementedException("OnlyHash");
            if (options.RawLeaves) throw new NotImplementedException("RawLeaves");
            if (options.Trickle) throw new NotImplementedException("Trickle");

            // Build the DAG.
            var dm = new DataMessage
            {
                Type = DataType.File,
                Data = data,
                FileSize = (ulong)data.Length,
            };
            var pb = new MemoryStream();
            ProtoBuf.Serializer.Serialize<DataMessage>(pb, dm);
            var dag = new DagNode(pb.ToArray(), null, options.Hash);

            // Save it.
            var cid = await ipfs.Block.PutAsync(
                data: dag.ToArray(), 
                multiHash: options.Hash,
                pin: options.Pin,
                cancel: cancel);

            // Wrap in directory?
            if (options.Wrap)
            {
                var link = dag.ToLink(name);
                var links = new FileSystemLink[] 
                {
                    new FileSystemLink
                    {
                        Id = link.Id,
                        Name = link.Name,
                        Size = link.Size
                    }
                };
                return await CreateDirectoryAsync(links, options, cancel);
            }

            // Return the file system node.
            return new FileSystemNode
            {
                Id = cid,
                Name = name,
                IsDirectory = false,
                Size = data.Length,
                Links = FileSystemLink.None
            };
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

            return new MemoryStream(buffer: dm.Data, writable: false);
        }
    }
}
