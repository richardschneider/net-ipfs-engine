using System;
using Ipfs;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.Discovery
{
    /// <summary>
    ///   The event data.
    /// </summary>
    public class PeerDiscoveredEventArgs : EventArgs
    {
        /// <summary>
        ///   The address of the peer that was discovered.
        /// </summary>
        /// <value>
        ///   The address must end with the ipfs protocol and the public ID
        ///   of the peer.  For example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
        /// </value>
        public MultiAddress Address { get; set; }
    }
}
