using Ipfs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer.Transports
{
    /// <summary>
    ///   Establishes a duplex stream between two peers
    ///   over a specific network transport.
    /// </summary>
    public interface IPeerTransport
    {
        /// <summary>
        ///   Connect to a peer.
        /// </summary>
        /// <param name="address">
        ///   The address of the peer.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, a <b>null</b> is returned.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is a duplex <see cref="Stream"/> or <b>null</b>.
        /// </returns>
        Task<Stream> ConnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken));
    }
}
