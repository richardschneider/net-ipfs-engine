using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine.UnixFileSystem
{
    /// <summary>
    ///   Provides read only access to a slice of stream.
    /// </summary>
    class SlicedStream : Stream
    {
        Stream stream;
        long offset;
        long logicalEnd;

        public SlicedStream(Stream stream, long offset, long count)
        {
            this.stream = stream;
            this.offset = offset;

            stream.Position = offset;
            logicalEnd = count < 1 
                ? stream.Length
                : Math.Min(stream.Length, offset + count);
        }

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => stream.Length;

        public override bool CanTimeout => stream.CanTimeout;

        public override int ReadTimeout { get => stream.ReadTimeout; set => stream.ReadTimeout = value; }

        public override int WriteTimeout { get => stream.WriteTimeout; set => stream.WriteTimeout = value; }

        public override void Flush()
        {
            stream.Flush();
        }

        public override long Position {
            get => stream.Position - offset;
            set => throw new NotSupportedException();
        }

        public override int ReadByte()
        {
            if (stream.Position >= logicalEnd)
                return -1;
            return stream.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (stream.Position >= logicalEnd)
                return 0;
            var length = Math.Min(count, logicalEnd - stream.Position);
            var n = stream.Read(buffer, offset, (int) length);
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return stream.FlushAsync(cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }
    }
}
