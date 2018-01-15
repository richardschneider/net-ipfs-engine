using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    /// <summary>
    ///   Configuration options for the <see cref="IpfsEngine"/>.
    /// </summary>
    public class IpfsEngineOptions
    {
        /// <summary>
        ///   Repository options.
        /// </summary>
        public RepositoryOptions Repository { get; set; } = new RepositoryOptions();
    }
}
