using Ipfs;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Protocols
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
        ///   Process a message for the protocol.
        /// </summary>
        /// <param name="connection">
        ///   A connection between two peers.
        /// </param>
        /// <param name="stream">
        ///   The message source.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task ProcessMessageAsync(PeerConnection connection, Stream stream, CancellationToken cancel = default(CancellationToken));

    }
}
