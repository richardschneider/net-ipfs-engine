using Common.Logging;
using Ipfs;
using PeerTalk.Protocols;
using ProtoBuf;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.SecureCommunication
{
    /// <summary>
    ///   Creates a secure connection with a peer.
    /// </summary>
    public class Secio1 : IEncryptionProtocol
    {
        static ILog log = LogManager.GetLogger(typeof(Secio1));

        /// <inheritdoc />
        public string Name { get; } = "secio";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1, 0);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"/{Name}/{Version}";
        }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection, Stream stream, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException("SECIO is NYI.");
        }

        public Task<Stream> EncryptAsync(PeerConnection connection, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException("SECIO is NYI.");
        }

    }
}
