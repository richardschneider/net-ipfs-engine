using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ipfs.Engine.Cryptography;
using Makaretu.Dns;

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

        /// <summary>
        ///   Provides access to the Domain Name System.
        /// </summary>
        /// <value>
        ///   Defaults to <see cref="DotClient">DNS over TLS</see>.
        /// </value>
        public IDnsClient Dns { get; set; } = new DotClient();

        /// <summary>
        ///   Block options.
        /// </summary>
        public BlockOptions Block { get; set; } = new BlockOptions();

        /// <summary>
        ///    Discovery options.
        /// </summary>
        public DiscoveryOptions Discovery { get; set; } = new DiscoveryOptions();
    }
}
