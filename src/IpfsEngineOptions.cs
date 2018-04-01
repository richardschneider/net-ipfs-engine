using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ipfs.Engine.Cryptography;

namespace Ipfs.Engine
{
    /// <summary>
    ///   Configuration options for the <see cref="IpfsEngine"/>.
    /// </summary>
    /// <seealso cref="IpfsEngine.Options"/>
    public class IpfsEngineOptions
    {
        /// <summary>
        ///   Repository options.
        /// </summary>
        public RepositoryOptions Repository { get; set; } = new RepositoryOptions();

        /// <summary>
        ///   KeyChain options.
        /// </summary>
        public KeyChainOptions KeyChain { get; set; } = new KeyChainOptions();
    }
}
