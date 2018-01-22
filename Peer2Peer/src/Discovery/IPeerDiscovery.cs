using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peer2Peer.Discovery
{
    /// <summary>
    ///   Describes a service that finds a peer.
    /// </summary>
    /// <remarks>
    ///   All discovery services must raise the <see cref="PeerDiscovered"/> event.
    /// </remarks>
    public interface IPeerDiscovery : IService
    {
        /// <summary>
        ///   Raised when a peer is discovered.
        /// </summary>
        event EventHandler<PeerDiscoveredEventArgs> PeerDiscovered;
    }
}
