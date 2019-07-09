using ICSharpCode.SharpZipLib.Tar;
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

        static readonly int DefaultLinksPerBlock = 174;

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
                return await AddAsync(stream, Path.GetFileName(path), options, cancel).ConfigureAwait(false);
            }
        }

        public async Task<IFileSystemNode> AddTextAsync(
            string text,
            AddFileOptions options = default(AddFileOptions),
            CancellationToken cancel = default(CancellationToken))
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(text), false))
            {
                return await AddAsync(ms, "", options, cancel).ConfigureAwait(false);
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
            if (options.Trickle) throw new NotImplementedException("Trickle");

            var blockService = GetBlockService(options);
            var keyChain = await ipfs.KeyChainAsync(cancel).ConfigureAwait(false);

            var chunker = new SizeChunker();
            var nodes = await chunker.ChunkAsync(stream, name, options, blockService, keyChain, cancel).ConfigureAwait(false);

            // Multiple nodes for the file?
            FileSystemNode node = await BuildTreeAsync(nodes, options, cancel);

            // Wrap in directory?
            if (options.Wrap)
            {
                var link = node.ToLink(name);
                var wlinks = new IFileSystemLink[] { link };
                node = await CreateDirectoryAsync(wlinks, options, cancel).ConfigureAwait(false);
            }
            else
            {
                node.Name = name;
            }

            // Advertise the root node.
            if (options.Pin && ipfs.IsStarted)
            {
                await ipfs.Dht.ProvideAsync(node.Id, advertise: true, cancel: cancel).ConfigureAwait(false);
            }

            // Return the file system node.
            return node;
        }

        async Task<FileSystemNode> BuildTreeAsync(
            IEnumerable<FileSystemNode> nodes,
            AddFileOptions options,
            CancellationToken cancel)
        {
            if (nodes.Count() == 1)
            {
                return nodes.First();
            }

            // Bundle X links into a block.
            var tree = new List<FileSystemNode>();
            for (int i = 0; true; ++i)
            {
                var bundle = nodes
                    .Skip(DefaultLinksPerBlock * i)
                    .Take(DefaultLinksPerBlock);
                if (bundle.Count() == 0)
                {
                    break;
                }
                var node = await BuildTreeNodeAsync(bundle, options, cancel);
                tree.Add(node);
            }
            return await BuildTreeAsync(tree, options, cancel);
        }

        async Task<FileSystemNode> BuildTreeNodeAsync(
            IEnumerable<FileSystemNode> nodes,
            AddFileOptions options,
            CancellationToken cancel)
        {
            var blockService = GetBlockService(options);

            // Build the DAG that contains all the file nodes.
            var links = nodes.Select(n => n.ToLink()).ToArray();
            var fileSize = (ulong)nodes.Sum(n => n.Size);
            var dm = new DataMessage
            {
                Type = DataType.File,
                FileSize = fileSize,
                BlockSizes = nodes.Select(n => (ulong)n.Size).ToArray()
            };
            var pb = new MemoryStream();
            ProtoBuf.Serializer.Serialize<DataMessage>(pb, dm);
            var dag = new DagNode(pb.ToArray(), links, options.Hash);

            // Save it.
            dag.Id = await blockService.PutAsync(
                data: dag.ToArray(),
                multiHash: options.Hash,
                encoding: options.Encoding,
                pin: options.Pin,
                cancel: cancel).ConfigureAwait(false);

            return new FileSystemNode
            {
                Id = dag.Id,
                Size = (long)dm.FileSize,
                DagSize = dag.Size,
                Links = links
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
            var nodes = await Task.WhenAll(files).ConfigureAwait(false);

            // Create the DAG with links to the created files and sub-directories
            var links = nodes
                .Select(node => node.ToLink())
                .ToArray();
            var fsn = await CreateDirectoryAsync(links, options, cancel).ConfigureAwait(false);
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
            var cid = await GetBlockService(options).PutAsync(
                data: dag.ToArray(),
                multiHash: options.Hash,
                encoding: options.Encoding,
                pin: options.Pin,
                cancel: cancel).ConfigureAwait(false);

            return new FileSystemNode
            {
                Id = cid,
                Links = links,
                IsDirectory = true
            };
        }

        public async Task<IFileSystemNode> ListFileAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            var cid = await ipfs.ResolveIpfsPathToCidAsync(path, cancel).ConfigureAwait(false);
            var block = await ipfs.Block.GetAsync(cid, cancel).ConfigureAwait(false);

            // TODO: A content-type registry should be used.
            if (cid.ContentType == "dag-pb")
            {
                // fall thru
            }
            else if (cid.ContentType == "raw")
            {
                return new FileSystemNode
                {
                    Id = cid,
                    Size = block.Size
                };
            }
            else if (cid.ContentType == "cms")
            {
                return new FileSystemNode
                {
                    Id = cid,
                    Size = block.Size
                };
            }
            else
            {
                throw new NotSupportedException($"Cannot read content type '{cid.ContentType}'.");
            }

            var dag = new DagNode(block.DataStream);
            var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);
            var fsn = new FileSystemNode
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
                Size = (long)(dm.FileSize ?? 0)
            };

            // Cannot determine if a link points to a directory.  The link's block must be
            // read to get this info.
            if (fsn.IsDirectory)
            {
                foreach (FileSystemLink link in fsn.Links)
                {
                    var lblock = await ipfs.Block.GetAsync(link.Id, cancel).ConfigureAwait(false);
                    var ldag = new DagNode(lblock.DataStream);
                    var ldm = Serializer.Deserialize<DataMessage>(ldag.DataStream);
                    link.IsDirectory = ldm.Type == DataType.Directory;
                }
            }

            return fsn;
        }

        public async Task<string> ReadAllTextAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            using (var data = await ReadFileAsync(path, cancel).ConfigureAwait(false))
            using (var text = new StreamReader(data))
            {
                return await text.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        public async Task<Stream> ReadFileAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            var cid = await ipfs.ResolveIpfsPathToCidAsync(path, cancel).ConfigureAwait(false);
            var keyChain = await ipfs.KeyChainAsync(cancel).ConfigureAwait(false);
            return await FileSystem.CreateReadStreamAsync(cid, ipfs.Block, keyChain, cancel).ConfigureAwait(false);
        }

        public async Task<Stream> ReadFileAsync(string path, long offset, long count = 0, CancellationToken cancel = default(CancellationToken))
        {
            var stream = await ReadFileAsync(path, cancel).ConfigureAwait(false);
            return new SlicedStream(stream, offset, count);
        }

        public async Task<Stream> GetAsync(string path, bool compress = false, CancellationToken cancel = default(CancellationToken))
        {
            var cid = await ipfs.ResolveIpfsPathToCidAsync(path, cancel).ConfigureAwait(false);
            var ms = new MemoryStream();
            using (var tarStream = new TarOutputStream(ms, 1))
            using (var archive = TarArchive.CreateOutputTarArchive(tarStream))
            {
                archive.IsStreamOwner = false;
                await AddTarNodeAsync(cid, cid.Encode(), tarStream, cancel).ConfigureAwait(false);
            }
            ms.Position = 0;
            return ms;
        }

        async Task AddTarNodeAsync(Cid cid, string name, TarOutputStream tar, CancellationToken cancel)
        {
            var block = await ipfs.Block.GetAsync(cid, cancel).ConfigureAwait(false);
            var dm = new DataMessage { Type = DataType.Raw };
            DagNode dag = null;

            if (cid.ContentType == "dag-pb")
            {
                dag = new DagNode(block.DataStream);
                dm = Serializer.Deserialize<DataMessage>(dag.DataStream);
            }
            var entry = new TarEntry(new TarHeader());
            var header = entry.TarHeader;
            header.Mode = 0x1ff; // 777 in octal
            header.LinkName = String.Empty;
            header.UserName = String.Empty;
            header.GroupName = String.Empty;
            header.Version = "00";
            header.Name = name;
            header.DevMajor = 0;
            header.DevMinor = 0;
            header.UserId = 0;
            header.GroupId = 0;
            header.ModTime = DateTime.Now;

            if (dm.Type == DataType.Directory)
            {
                header.TypeFlag = TarHeader.LF_DIR;
                header.Size = 0;
                tar.PutNextEntry(entry);
                tar.CloseEntry();
            }
            else // Must be a file
            {
                var content = await ReadFileAsync(cid, cancel).ConfigureAwait(false);
                header.TypeFlag = TarHeader.LF_NORMAL;
                header.Size = content.Length;
                tar.PutNextEntry(entry);
                await content.CopyToAsync(tar);
                tar.CloseEntry();
            }

            // Recurse over files and subdirectories
            if (dm.Type == DataType.Directory)
            {
                foreach (var link in dag.Links)
                {
                    await AddTarNodeAsync(link.Id, $"{name}/{link.Name}", tar, cancel).ConfigureAwait(false);
                }
            }
        }


        IBlockApi GetBlockService(AddFileOptions options)
        {
            return options.OnlyHash
                ? new HashOnlyBlockService()
                : ipfs.Block;
        }

        /// <summary>
        ///   A Block service that only computes the block's hash.
        /// </summary>
        class HashOnlyBlockService : IBlockApi
        {
            public Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<Cid> PutAsync(
                byte[] data, 
                string contentType = Cid.DefaultContentType, 
                string multiHash = MultiHash.DefaultAlgorithmName,
                string encoding = MultiBase.DefaultAlgorithmName,
                bool pin = false, 
                CancellationToken cancel = default(CancellationToken))
            {
                var cid = new Cid
                {
                    ContentType = contentType,
                    Encoding = encoding,
                    Hash = MultiHash.ComputeHash(data, multiHash),
                    Version = (contentType == "dag-pb" && multiHash == "sha2-256") ? 0 : 1
                };
                return Task.FromResult(cid);
            }

            public Task<Cid> PutAsync(
                Stream data,
                string contentType = Cid.DefaultContentType,
                string multiHash = MultiHash.DefaultAlgorithmName,
                string encoding = MultiBase.DefaultAlgorithmName,
                bool pin = false, 
                CancellationToken cancel = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<Cid> RemoveAsync(Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public Task<IDataBlock> StatAsync(Cid id, CancellationToken cancel = default(CancellationToken))
            {
                throw new NotImplementedException();
            }
        }
    }
}
