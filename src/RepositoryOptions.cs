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
    public class RepositoryOptions
    {
        /// <summary>
        ///   The directory of the repository.
        /// </summary>
        public string Folder { get; set; }
            = Path.Combine(
                Environment.GetEnvironmentVariable("HOME") ??
                Environment.GetEnvironmentVariable("HOMEPATH"),
                ".csipfs");

        /// <summary>
        ///   The fully qualified name of the database.
        /// </summary>
        public string DatabaseName
        {
            get { return Path.Combine(Folder, "ipfs.db"); }
        }
    }
}
