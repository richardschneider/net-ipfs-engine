using Common.Logging;
using Ipfs;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Multiplex
{
    /// <summary>
    ///   Supports multiple protocols over a single channel (stream).
    /// </summary>
    public class Muxer
    {
        static ILog log = LogManager.GetLogger(typeof(Muxer));

        /// <summary>
        ///   The next stream ID to create.
        /// </summary>
        /// <value>
        ///   The session initiator allocates odd IDs and the session receiver allocates even IDs.
        /// </value>
        public long NextStreamId { get; private set; }

        /// <summary>
        ///   The signle channel to exchange protocol messages.
        /// </summary>
        /// <value>
        ///   A <see cref="Stream"/> to exchange protocol messages.
        /// </value>
        public Stream Channel { get; set; }

        readonly AsyncLock ChannelWriteLock = new AsyncLock();
        
        /// <summary>
        ///   The substreams that are open.
        /// </summary>
        /// <value>
        ///   The key is stream ID and the value is a <see cref="Substream"/>.
        /// </value>
        public ConcurrentDictionary<long, Substream> Substreams = new ConcurrentDictionary<long, Substream>();

        /// <summary>
        ///   Determines if the muxer is the initiator.
        /// </summary>
        /// <value>
        ///   <b>true</b> if the muxer is the initiator.
        /// </value>
        /// <seealso cref="Receiver"/>
        public bool Initiator
        {
            get { return (NextStreamId & 1) == 1; }
            set
            {
                if (value != Initiator)
                    NextStreamId += 1;
            }
        }

        /// <summary>
        ///   Determines if the muxer is the receiver.
        /// </summary>
        /// <value>
        ///   <b>true</b> if the muxer is the receiver.
        /// </value>
        /// <seealso cref="Initiator"/>
        public bool Receiver
        {
            get { return !Initiator; }
            set { Initiator = !value; }
        }

        /// <summary>
        ///   Creates a new stream with the specified name.
        /// </summary>
        /// <param name="name">
        ///   A name for the stream.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>TODO</returns>
        public async Task<Substream> CreateStreamAsync(string name = "", CancellationToken cancel = default(CancellationToken))
        {
            var streamId = NextStreamId;
            NextStreamId += 2;
            var substream = new Substream
            {
                Id = streamId,
                Name = name
            };
            Substreams.TryAdd(streamId, substream);

            // Tell the other side about the new stream.
            var header = new Header { StreamId = streamId, PacketType = PacketType.NewStream };
            var wireName = Encoding.UTF8.GetBytes(name);
            await header.WriteAsync(Channel, cancel);
            await Channel.WriteVarintAsync(wireName.Length, cancel);
            await Channel.WriteAsync(wireName, 0, wireName.Length);
            await Channel.FlushAsync();

            return substream;
        }

        public async Task ProcessRequestsAsync(CancellationToken cancel = default(CancellationToken))
        {
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    var header = await Header.ReadAsync(Channel, cancel);
                    var length = await Varint.ReadVarint32Async(Channel, cancel);
                    if (log.IsDebugEnabled)
                        log.DebugFormat("received '{0}', stream={1}, length={2}", header.PacketType, header.StreamId, length);

                    // Read the payload.
                    var payload = new byte[length];
                    int offset = 0;
                    while (offset < length)
                    {
                        offset += Channel.Read(payload, offset, length - offset);
                    }
                    var substream = Substreams[header.StreamId];

                    // Process the packet
                    switch (header.PacketType)
                    {
                        case PacketType.NewStream:
                            substream = new Substream
                            {
                                Id = header.StreamId,
                                Name = Encoding.UTF8.GetString(payload)
                            };
                            Substreams.TryAdd(substream.Id, substream);
                            break;

                        case PacketType.MessageInitiator:
                        case PacketType.MessageReceiver:
                            if (substream == null)
                            {
                                log.Warn($"Message to unknown stream #{header.StreamId}");
                                continue;
                            }
                            substream.SetMessage(payload);
                            break;

                        default:
                            log.Debug($"Unknown Muxer packet type '{header.PacketType}'.");
                            break;
                            //throw new InvalidDataException($"Unknown Muxer packet type '{header.PacketType}'.");
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // eat it
                Channel.Dispose();
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // eat it
                Channel.Dispose();
            }
            catch (Exception e)
            {
                log.Error("failed", e);
                Channel.Dispose();
            }
        }

        /// <summary>
        ///   Acquire permission to write to the Channel.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous get operation. The task's value
        ///   is an <see cref="IDisposable"/> that releases the lock.
        /// </returns>
        public Task<IDisposable> AcquireWriteAccessAsync()
        {
            return ChannelWriteLock.LockAsync();
        }
    }
}
