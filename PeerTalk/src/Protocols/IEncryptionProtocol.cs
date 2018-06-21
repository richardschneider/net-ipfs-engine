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
    ///   Applies encryption to a <see cref="PeerConnection"/>.
    /// </summary>
    public interface IEncryptionProtocol : IPeerProtocol
    {
        /// <summary>
        ///   Creates an encrypted stream for the connection.
        /// </summary>
        /// <param name="connection">
        ///   A connection between two peers.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the encrypted stream.
        /// </returns>
        Task<Stream> EncryptAsync(PeerConnection connection, CancellationToken cancel = default(CancellationToken));
    }
}
