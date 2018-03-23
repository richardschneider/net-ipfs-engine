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
        public async Task ProcessRequestAsync(PeerConnection connection, CancellationToken cancel = default(CancellationToken))
        {
            try
            {
                while (!cancel.IsCancellationRequested && connection.Stream != null)
                {
                    var msg = await Message.ReadStringAsync(connection.Stream, cancel);

                    // TODO: msg == "ls"
                    if (msg == "ls")
                    {
                        throw new NotImplementedException("multistream ls");
                    }

                    // Switch the protocol
                    if (!ProtocolRegistry.Protocols.TryGetValue(msg, out Func<IPeerProtocol> maker))
                    {
                        await Message.WriteAsync("na", connection.Stream, cancel);
                        return;
                    }

                    // Ack protocol switch
                    log.Debug("switching to " + msg);
                    await Message.WriteAsync(msg, connection.Stream, cancel);

                    // Process protocol message.
                    var protocol = maker();
                    if (protocol.ToString() == this.ToString())
                    {
                        continue;
                    }
                    await protocol.ProcessRequestAsync(connection, cancel);
                    return;
                }
            }
            catch (EndOfStreamException)
            {
                // eat it
                if (connection != null)
                    connection.Dispose();
            }
            catch (Exception) when (cancel.IsCancellationRequested || connection.Stream == null)
            {
                // eat it
                if (connection != null)
                    connection.Dispose();
            }
            catch (Exception e)
            {
                log.Error("failed", e);
                if (connection != null)
                    connection.Dispose();
            }


        }

        /// <inheritdoc />
        public Task ProcessResponseAsync(PeerConnection connection, CancellationToken cancel = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }
}
