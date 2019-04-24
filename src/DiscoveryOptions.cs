using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    /// <summary>
    ///   Configuration options for discovering other peers.
    /// </summary>
    /// <seealso cref="IpfsEngineOptions"/>
    public class DiscoveryOptions
    {
        /// <summary>
        ///   Well known peers used to find other peers in
        ///   the IPFS network.
        /// </summary>
        /// <value>
        ///   The default value is <b>null</b>.
        /// </value>
        /// <remarks>
        ///   If not null, then the sequence is use by
        ///   the block API; otherwise the values in the configuration
        ///   file are used.
        /// </remarks>
        public IEnumerable<MultiAddress> BootstrapPeers;

        /// <summary>
        ///   Disables the multicast DNS discovery of other peers
        ///   and advertising of this peer.
        /// </summary>
        public bool DisableMdns;
    }
}
