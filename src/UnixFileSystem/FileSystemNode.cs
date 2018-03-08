using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine.UnixFileSystem
{
    class FileSystemNode : IFileSystemNode
    {
        public bool IsDirectory { get; set; }

        public IEnumerable<IFileSystemLink> Links { get; set; }

        public byte[] DataBytes { get; set; }

        public Stream DataStream { get; set; }

        public Cid Id { get; set; }

        public long Size { get; set; }

        public IFileSystemLink ToLink(string name = "")
        {
            throw new NotImplementedException();
        }
    }
}
