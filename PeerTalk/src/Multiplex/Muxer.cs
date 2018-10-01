using Common.Logging;
using Ipfs;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Multiplex
{
    /// <summary>
    ///   Supports multiple protocols over a single channel (stream).
    /// </summary>
    /// <remarks>
    ///   See <see href="https://github.com/libp2p/mplex"/> for the spec.
    /// </remarks>
    public class Muxer
    {
        static ILog log = LogManager.GetLogger(typeof(Muxer));

        /// <summary>
        ///   The next stream ID to create.
        /// </summary>
        /// <value>
        ///   The session initiator allocates even IDs and the session receiver allocates odd IDs.
        /// </value>
        public long NextStreamId { get; private set; }

        /// <summary>
        ///   The signle channel to exchange protocol messages.
        /// </summary>
        /// <value>
        ///   A <see cref="Stream"/> to exchange protocol messages.
        /// </value>
        public Stream Channel { get; set; }

        /// <summary>
        ///   The peer connection.
        /// </summary>
        /// <value>
        ///   The peer connection that owns this muxer.
        /// </value>
        public PeerConnection Connection { get; set; }

        /// <summary>
        ///   Raised when the remote end creates a new stream.
        /// </summary>
        public event EventHandler<Substream> SubstreamCreated;

        /// <summary>
        ///   Raised when the remote end closes a stream.
        /// </summary>
        public event EventHandler<Substream> SubstreamClosed;

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
            get { return (NextStreamId & 1) == 0; }
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
        /// <returns>
        ///   A duplex stream.
        /// </returns>
        public async Task<Substream> CreateStreamAsync(string name = "", CancellationToken cancel = default(CancellationToken))
        {
            var streamId = NextStreamId;
            NextStreamId += 2;
            var substream = new Substream
            {
                Id = streamId,
                Name = name,
                Muxer = this,
                SentMessageType = PacketType.MessageInitiator,
            };
            Substreams.TryAdd(streamId, substream);

            // Tell the other side about the new stream.
            using (await AcquireWriteAccessAsync())
            {
                var header = new Header { StreamId = streamId, PacketType = PacketType.NewStream };
                var wireName = Encoding.UTF8.GetBytes(name);
                await header.WriteAsync(Channel, cancel);
                await Channel.WriteVarintAsync(wireName.Length, cancel);
                await Channel.WriteAsync(wireName, 0, wireName.Length);
                await Channel.FlushAsync();
            }
            return substream;
        }

        /// <summary>
        ///   Remvove the stream.
        /// </summary>
        /// <remarks>
        ///   Internal method called by Substream.Dispose().
        /// </remarks>
        public async Task<Substream> RemoveStreamAsync(Substream stream, CancellationToken cancel = default(CancellationToken))
        {
            if (Substreams.TryRemove(stream.Id, out Substream _))
            {
                // Tell the other side.
                using (await AcquireWriteAccessAsync())
                {
                    var header = new Header
                    {
                        StreamId = stream.Id,
                        PacketType = PacketType.ResetInitiator
                    };
                    await header.WriteAsync(Channel, cancel);
                    Channel.WriteByte(0); // length
                    await Channel.FlushAsync();
                }
            }

            return stream;
        }

        /// <summary>
        ///   Read the multiplex packets.
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <remarks>
        ///   A background task that reads and processes the multiplex packets while
        ///   the <see cref="Channel"/> is open and not <paramref name="cancel">cancelled</paramref>.
        ///   <para>
        ///   Any encountered errors will close the <see cref="Channel"/>.
        ///   </para>
        /// </remarks>
        public async Task ProcessRequestsAsync(CancellationToken cancel = default(CancellationToken))
        {
            try
            {
                while (Channel.CanRead && !cancel.IsCancellationRequested)
                {
                    // Read the packet prefix.
                    var header = await Header.ReadAsync(Channel, cancel);
                    var length = await Varint.ReadVarint32Async(Channel, cancel);
                    if (log.IsDebugEnabled)
                        log.DebugFormat("received '{0}', stream={1}, length={2}", header.PacketType, header.StreamId, length);

                    // Read the payload.
                    var payload = new byte[length];
                    int offset = 0;
                    while (offset < length)
                    {
                        offset += await Channel.ReadAsync(payload, offset, length - offset, cancel);
                    }

                    // Process the packet
                    Substreams.TryGetValue(header.StreamId, out Substream substream);
                    switch (header.PacketType)
                    {
                        case PacketType.NewStream:
                            substream = new Substream
                            {
                                Id = header.StreamId,
                                Name = Encoding.UTF8.GetString(payload),
                                Muxer = this
                            };
                            if (!Substreams.TryAdd(substream.Id, substream))
                            {
                                throw new Exception($"Stream {substream.Id} already exists");
                            }
                            SubstreamCreated?.Invoke(this, substream);
                            break;

                        case PacketType.MessageInitiator:
                        case PacketType.MessageReceiver:
                            if (substream == null)
                            {
                                log.Warn($"Message to unknown stream #{header.StreamId}");
                                continue;
                            }
                            substream.AddData(payload);
                            break;

                        case PacketType.CloseInitiator:
                        case PacketType.CloseReceiver:
                        case PacketType.ResetInitiator:
                        case PacketType.ResetReceiver:
                            if (substream == null)
                            {
                                log.Warn($"Reset of unknown stream #{header.StreamId}");
                                continue;
                            }
                            substream.NoMoreData();
                            Substreams.TryRemove(substream.Id, out Substream _);
                            SubstreamClosed?.Invoke(this, substream);
                            break;

                        default:
                            throw new InvalidDataException($"Unknown Muxer packet type '{header.PacketType}'.");
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // eat it
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionReset)
            {
                // eat it
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // eat it
            }
            catch (Exception e)
            {
                // Log error if the channel is not closed.
                if (Channel.CanRead || Channel.CanWrite)
                {
                    log.Error("failed", e);
                }
            }

            Channel.Dispose();
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
