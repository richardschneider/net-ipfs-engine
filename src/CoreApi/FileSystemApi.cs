using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Ipfs.Engine.UnixFileSystem;
using ProtoBuf;

namespace Ipfs.Engine.CoreApi
{
    class FileSystemApi : IFileSystemApi
    {
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

        public Task<IFileSystemNode> AddDirectoryAsync(string path, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
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
                IsDirectory = false,
                Size = data.Length,
                Links = FileSystemLink.None
            };
        }

        public Task<IFileSystemNode> ListFileAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
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
            var dag = new DagNode(new MemoryStream(buffer: block.DataBytes, writable: false));
            var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);

            if (dm.Type != DataType.File)
                throw new Exception($"'{path} is not a file.");

            return new MemoryStream(buffer: dm.Data, writable: false);
        }
    }
}
