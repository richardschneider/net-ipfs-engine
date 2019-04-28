using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PeerTalk.Cryptography;

namespace Ipfs.Engine
{
    /// <summary>
    ///   Configuration options for communication with other peers.
    /// </summary>
    /// <seealso cref="IpfsEngineOptions"/>
    public class SwarmOptions
    {
        /// <summary>
        ///   The key of the private network.
        /// </summary>
        /// <value>
        ///   The key must either <b>null</b> or 32 bytes (256 bits) in length.
        /// </value>
        /// <remarks>
        ///   When null, the public network is used.  Otherwise, the network is
        ///   considered private and only peers with the same key will be
        ///   communicated with.
        ///   <para>
        ///   When using a private network, the <see cref="DiscoveryOptions.BootstrapPeers"/>
        ///   must also use this key.
        ///   </para>
        /// </remarks>
        /// <seealso href="https://github.com/libp2p/specs/blob/master/pnet/Private-Networks-PSK-V1.md"/>
        public PreSharedKey PrivateNetworkKey { get; set; }

        /// <summary>
        ///   The low water mark for peer connections.
        /// </summary>
        /// <value>
        ///   Defaults to 16.
        /// </value>
        /// <remarks>
        ///   The <see cref="PeerTalk.AutoDialer"/> is used to maintain at
        ///   least this number of connections.
        ///   <para>
        ///   Setting this to zero will disable the auto dial feature.
        ///   </para>
        /// </remarks>
        public int MinConnections { get; set; } = 16;

    }
}
