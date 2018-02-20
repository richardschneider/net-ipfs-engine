using Ipfs;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer.Protocols
{
    /// <summary>
    ///   Defines the messages that can be exchanged between two peers.
    /// </summary>
    /// <remarks>
    ///   <see cref="Object.ToString"/> must return a string in the form
    ///   "/name/version".
    /// </remarks>
    public interface IPeerProtocol
    {
        /// <summary>
        ///   The name of the protocol.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///   The version of the protocol.
        /// </summary>
        SemVersion Version { get; }

        /// <summary>
        ///   Process a request for the protocol.
        /// </summary>
        /// <param name="connection">
        ///   A connection to the peer.
        /// </param>
        /// <param name="cancel">
        /// </param>
        /// <returns>
        /// </returns>
        Task ProcessRequestAsync(PeerConnection connection, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Process a response from the protocol.
        /// </summary>
        /// <param name="connection">
        ///   A connection to the peer.
        /// </param>
        /// <param name="cancel">
        /// </param>
        /// <returns>
        /// </returns>
        Task ProcessResponseAsync(PeerConnection connection, CancellationToken cancel = default(CancellationToken));
    }
}
