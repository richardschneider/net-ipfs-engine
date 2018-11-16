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
    ///   TODO
    /// </summary>
    public class Plaintext1 : IEncryptionProtocol
    {
        /// <inheritdoc />
        public string Name { get; } = "plaintext";

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
            connection.SecurityEstablished.SetResult(true);
            await connection.EstablishProtocolAsync("/multistream/", CancellationToken.None);
        }

        /// <inheritdoc />
        public Task<Stream> EncryptAsync(PeerConnection connection, CancellationToken cancel = default(CancellationToken))
        {
            connection.SecurityEstablished.SetResult(true);
            return Task.FromResult(connection.Stream);
        }

    }
}
