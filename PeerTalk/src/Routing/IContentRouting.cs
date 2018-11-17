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
    ///    Find information about who has what content.
    /// </summary>
    /// <remarks>
    ///   No IPFS documentation is currently available.  See the 
    ///   <see href="https://godoc.org/github.com/libp2p/go-libp2p-routing">code</see>.
    /// </remarks>
    public interface IContentRouting
    {
        /// <summary>
        ///    Adds the <see cref="Cid"/> to the content routing system.
        /// </summary>
        /// <param name="cid">
        ///   The ID of some content that the peer contains.
        /// </param>
        /// <param name="advertise">
        ///   Advertise the <paramref name="cid"/> to other peers.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task ProvideAsync(Cid cid, bool advertise = true, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Find the providers for the specified content.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the content.
        /// </param>
        /// <param name="limit">
        ///   The maximum number of peers to return.  Defaults to 20.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation that returns
        ///   a sequence of IPFS <see cref="Peer"/>.
        /// </returns>
        Task<IEnumerable<Peer>> FindProvidersAsync(Cid id, int limit = 20, CancellationToken cancel = default(CancellationToken));
    }
}

