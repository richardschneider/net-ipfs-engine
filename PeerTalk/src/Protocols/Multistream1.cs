using Common.Logging;
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
    ///   A protocol to select other protocols.
    /// </summary>
    /// <seealso href="https://github.com/multiformats/multistream-select"/>
    public class Multistream1 : IPeerProtocol
    {
        static ILog log = LogManager.GetLogger(typeof(Multistream1));

        /// <inheritdoc />
        public string Name { get; } = "multistream";

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
            var msg = await Message.ReadStringAsync(stream, cancel);

            // TODO: msg == "ls"
            if (msg == "ls")
            {
                throw new NotImplementedException("multistream ls");
            }

            // Switch to the specified protocol
            if (!ProtocolRegistry.Protocols.TryGetValue(msg, out Func<IPeerProtocol> maker))
            {
                await Message.WriteAsync("na", stream, cancel);
                return;
            }

            // Ack protocol switch
            log.Debug("switching to " + msg);
            await Message.WriteAsync(msg, stream, cancel);

            // Process protocol message.
            var protocol = maker();
            await protocol.ProcessMessageAsync(connection, stream, cancel);
        }

    }
}
