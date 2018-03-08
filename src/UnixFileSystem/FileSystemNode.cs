using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine.UnixFileSystem
{
    /// <summary>
    ///   A node in the IPFS Unix File System.
    /// </summary>
    /// <remarks>
    ///   A <b>FileSystemNode</b> is either a directory or a file
    ///   <para>
    ///   A directory's <see cref="Links"/> is a sequence of files/directories
    ///   belonging to the directory.
    ///   </para>
    /// </remarks>
    public class FileSystemNode : IFileSystemNode
    {
        /// <inheritdoc />
        public bool IsDirectory { get; set; }

        /// <inheritdoc />
        public IEnumerable<IFileSystemLink> Links { get; set; }

        /// <inheritdoc />
        public byte[] DataBytes { get; set; }

        /// <inheritdoc />
        public Stream DataStream { get; set; }

        /// <inheritdoc />
        public Cid Id { get; set; }

        /// <inheritdoc />
        public long Size { get; set; }

        /// <inheritdoc />
        public IFileSystemLink ToLink(string name = "")
        {
            return new FileSystemLink
            {
                Name = name,
                Id = Id,
                Size = Size,
                IsDirectory = IsDirectory
            };
        }
    }
}
