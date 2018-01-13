using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class FileSystemApi : IFileSystemApi
    {
        IpfsEngine ipfs;

        public FileSystemApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<IFileSystemNode> AddAsync(Stream stream, string name = "", CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IFileSystemNode> AddDirectoryAsync(string path, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IFileSystemNode> AddFileAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IFileSystemNode> AddTextAsync(string text, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IFileSystemNode> ListFileAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<string> ReadAllTextAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Stream> ReadFileAsync(string path, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
