using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Routing
{
    /// <summary>
    ///    Find information about a peers.
    /// </summary>
    /// <remarks>
    ///   No IPFS documentation is currently available.  See the 
    ///   <see href="https://godoc.org/github.com/libp2p/go-libp2p-routing">code</see>.
    /// </remarks>
    public interface IPeerRouting
    {
        /// <summary>
        ///   Information about an IPFS peer.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="MultiHash"/> ID of the IPFS peer.  
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation that returns
        ///   the <see cref="Peer"/> information or a closer peer.
        /// </returns>
        Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default(CancellationToken));

    }
}

