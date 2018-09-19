using Ipfs;
using PeerTalk.Protocols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Multiplex
{
    /// <summary>
    ///   A substream used by the <see cref="Muxer"/>.
    /// </summary>
    public class Substream : Stream
    {
        Stream inStream;
        Stream outStream = new MemoryStream();

        public IPeerProtocol MessageHandler = new Multistream1();

        /// <summary>
        ///   The stream identifier.
        /// </summary>
        /// <value>
        ///   The session initiator allocates odd IDs and the session receiver allocates even IDs.
        /// </value>
        public long Id;
        
        /// <summary>
        ///   A name for the stream.
        /// </summary>
        /// <value>
        ///   Names do not need to be unique.
        /// </value>
        public string Name;

        /// <summary>
        ///   The multiplexor associated with the substream.
        /// </summary>
        public Muxer Muxer { get; set; }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override bool CanTimeout => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void SetMessage(byte[] message)
        {
            inStream = new MemoryStream(message, writable: false);
            outStream.Position = 0;
            outStream.SetLength(0);
            MessageHandler?.ProcessMessageAsync(Muxer.Connection, this);
        }

        /// <inheritdoc />
        public override int ReadByte()
        {
            return inStream.ReadByte();
        }

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return inStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return inStream.Read(buffer, offset, count);
        }

        
        /// <inheritdoc />
        public override void Flush()
        {
            FlushAsync().Wait();
        }

        /// <inheritdoc />
        public override async Task FlushAsync(CancellationToken cancel)
        {
            if (outStream.Length == 0)
                return;

            // Send the response over the muxer channel
            using (await Muxer.AcquireWriteAccessAsync())
            {
                outStream.Position = 0;
                var header = new Header
                {
                    StreamId = Id,
                    PacketType = Muxer.Initiator ? PacketType.MessageInitiator : PacketType.MessageReceiver
                };
                await header.WriteAsync(Muxer.Channel, cancel);
                await Varint.WriteVarintAsync(Muxer.Channel, outStream.Length, cancel);
                await outStream.CopyToAsync(Muxer.Channel);
                await Muxer.Channel.FlushAsync(cancel);

                outStream.SetLength(0);
            }
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            outStream.Write(buffer, offset, count);
        }

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return outStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc />
        public override void WriteByte(byte value)
        {
            outStream.WriteByte(value);
        }
    }

}

