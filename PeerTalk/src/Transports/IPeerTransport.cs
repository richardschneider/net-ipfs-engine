using Ipfs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Transports
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

        /// <summary>
        ///   Listen to any peer connections on the specified address.
        /// </summary>
        /// <param name="address">
        ///   The address to listen on.
        /// </param>
        /// <param name="handler">
        ///   The action to perform when a peer connection is received.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the connection listener.  When cancelled, the <see cref="OperationCanceledException"/>
        ///   is <b>NOT</b> raised.
        /// </param>
        /// <returns>
        ///   The actual address of the listener.
        /// </returns>
        /// <remarks>
        ///   The <paramref name="handler"/> is invoked on the peer listener thread. If
        ///   it throws, then the connection is closed but the listener remains
        ///   active.  It is passed a duplex stream, the local address and the remote
        ///   address.
        ///   <para>
        ///   To stop listening, the <paramref name="cancel"/> parameter 
        ///   must be supplied and then use the <see cref="CancellationTokenSource.Cancel()"/>
        ///   method.
        ///   </para>
        ///   <para>
        ///   For socket based transports (tcp or upd), if the port is not defined 
        ///   or is zero an ephermal port is assigned.
        ///   </para>
        /// </remarks>
        MultiAddress Listen(MultiAddress address, Action<Stream, MultiAddress, MultiAddress> handler, CancellationToken cancel);
    }
}
