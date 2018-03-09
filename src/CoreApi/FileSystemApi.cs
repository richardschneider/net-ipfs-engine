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

        public async Task<IFileSystemNode> AddAsync(Stream stream, string name = "", CancellationToken cancel = default(CancellationToken))
        {
            // TODO: If stream is seekable we can use .Length
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms, 8 * 1024);
                return await AddAsync(ms.ToArray(), name, cancel);
            }
        }

        public async Task<IFileSystemNode> AddFileAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return await AddAsync(stream, Path.GetFileName(path), cancel);
            }
        }

        public Task<IFileSystemNode> AddTextAsync(string text, CancellationToken cancel = default(CancellationToken))
        {
            return AddAsync(Encoding.UTF8.GetBytes(text), "", cancel);
        }

        public async Task<IFileSystemNode> AddAsync(byte[] data, string name, CancellationToken cancel)
        {
            // Build the DAG.
            var dm = new DataMessage
            {
                Type = DataType.File,
                Data = data,
                FileSize = (ulong)data.Length,
            };
            var pb = new MemoryStream();
            ProtoBuf.Serializer.Serialize<DataMessage>(pb, dm);
            var dag = new DagNode(pb.ToArray());

            // Save it.
            // TODO: Should be pinned
            var cid = await ipfs.Block.PutAsync(data: dag.ToArray(), cancel: cancel);

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

        public async Task<IFileSystemNode> AddDirectoryAsync(string path, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            // Add the files and sub-directories.
            path = Path.GetFullPath(path);
            var files = Directory
                .EnumerateFiles(path)
                .Select(p => AddFileAsync(p, cancel));
            if (recursive)
            {
                var folders = Directory
                    .EnumerateDirectories(path)
                    .Select(dir => AddDirectoryAsync(dir, recursive, cancel));
                files = files.Union(folders);
            }
            var nodes = await Task.WhenAll(files);

            // Create the DAG with links to the created files and sub-directories
            var links = nodes
                .Select(node => node.ToLink())
                .ToArray();
            var dm = new DataMessage { Type = DataType.Directory };
            var pb = new MemoryStream();
            ProtoBuf.Serializer.Serialize<DataMessage>(pb, dm);
            var dag = new DagNode(pb.ToArray(), links);

            // Save it.
            // TODO: Should be pinned
            var cid = await ipfs.Block.PutAsync(data: dag.ToArray(), cancel: cancel);

            return new FileSystemNode
            {
                Id = cid,
                Name = Path.GetFileName(path),
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
