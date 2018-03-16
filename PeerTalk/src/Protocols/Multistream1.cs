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
            log.Debug("start processing requests from " + connection.RemoteAddress);

            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    var msg = await Message.ReadStringAsync(connection.Stream, cancel);

                    // TODO: msg == "ls"
                    if (msg == "ls")
                    {
                        throw new NotImplementedException("multistream ls");
                    }

                    // Switch the protocol
                    if (!ProtocolRegistry.Protocols.TryGetValue(msg, out IPeerProtocol protocol))
                    {
                        await Message.WriteAsync("na", connection.Stream, cancel);
                        continue;
                    }

                    // Ack protocol switch
                    log.Debug("switching to " + msg);
                    await Message.WriteAsync(msg, connection.Stream, cancel);

                    // Start processing messages
                    if (protocol.ToString() != this.ToString())
                    {
                        await protocol.ProcessRequestAsync(connection, cancel);
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // eat it
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // eat it
            } 
            catch (Exception e)
            {
                log.Error("failed", e);
            }


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
