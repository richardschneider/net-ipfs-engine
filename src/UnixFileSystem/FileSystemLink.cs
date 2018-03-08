using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine.UnixFileSystem
{
    class FileSystemLink : IFileSystemLink
    {
        public static readonly FileSystemLink[] None = new FileSystemLink[0];

        public bool IsDirectory { get; set; }

        public string Name { get; set; }

        public Cid Id { get; set; }

        public long Size { get; set; }
}
}
