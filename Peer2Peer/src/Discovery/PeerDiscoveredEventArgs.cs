using System;
using Ipfs;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peer2Peer.Discovery
{
    /// <summary>
    ///   The event data.
    /// </summary>
    public class PeerDiscoveredEventArgs : EventArgs
    {
        /// <summary>
        ///   The peer that was discovered.
        /// </summary>
        /// <value>
        ///   A peer with an ID and at least one multiaddress.
        /// </value>
        public Peer Peer { get; set; }
    }
}
