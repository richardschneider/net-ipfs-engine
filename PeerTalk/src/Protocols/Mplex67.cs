using Ipfs;
using Common.Logging;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PeerTalk.Multiplex;

namespace PeerTalk.Protocols
{
    /// <summary>
    ///    A Stream Multiplexer protocol.
    /// </summary>
    /// <seealso href="https://github.com/libp2p/mplex"/>
    public class Mplex67 : IPeerProtocol
    {
        static ILog log = LogManager.GetLogger(typeof(Mplex67));

        /// <inheritdoc />
        public string Name { get; } = "mplex";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(6, 7);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"/{Name}/{Version}";
        }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection, CancellationToken cancel = default(CancellationToken))
        {
            log.Debug("start processing requests from " + connection.RemoteAddress);
            var muxer = new Muxer { Channel = connection.Stream, Initiator = true };

            await muxer.CreateStreamAsync();
            await muxer.ProcessRequestsAsync();

            // TODO: Attach muxer to the connection.  It now becomes
            // the message reader.

            log.Debug("stop processing from " + connection.RemoteAddress);
            connection.Dispose();
        }

        /// <inheritdoc />
        public Task ProcessResponseAsync(PeerConnection connection, CancellationToken cancel = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }
}
