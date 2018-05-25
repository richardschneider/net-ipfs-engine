using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    /// <summary>
    ///   Configuration options for the <see cref="Repository"/>.
    /// </summary>
    /// <seealso cref="IpfsEngineOptions"/>
    public class RepositoryOptions
    {
        /// <summary>
        ///   Creates a new instance of the <see cref="RepositoryOptions"/> class
        ///   with the default values.
        /// </summary>
        public RepositoryOptions()
        {
            var path = Environment.GetEnvironmentVariable("IPFS_PATH");
            if (path != null)
            {
                Folder = path;
            }
            else
            {
                Folder = Path.Combine(
                    Environment.GetEnvironmentVariable("HOME") ??
                    Environment.GetEnvironmentVariable("HOMEPATH"),
                    ".csipfs");
            }
        }

        /// <summary>
        ///   The directory of the repository.
        /// </summary>
        /// <value>
        ///   The default value is <c>$IPFS_PATH</c> or <c>$HOME/.csipfs</c> or
        ///   <c>$HOMEPATH/.csipfs</c>.
        /// </value>
        public string Folder { get; set; }

        /// <summary>
        ///   The fully qualified name of the database.
        /// </summary>
        /// <value>
        ///  "ipfs.db" in the <see cref="Folder"/>.
        /// </value>
        public string DatabaseName
        {
            get { return Path.Combine(Folder, "ipfs.db"); }
        }
    }
}
