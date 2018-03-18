using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   A simple wrapper around another stream that records statistics.
    /// </summary>
    class StatsStream : Stream
    {
        Stream stream;
        long bytesRead;
        long bytesWritten;
        DateTime lastUsed;

        public StatsStream(Stream stream)
        {
            this.stream = stream;
        }

        public long BytesRead => bytesRead;

        public long BytesWritten => bytesWritten;

        public DateTime LastUsed => lastUsed;

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override bool CanTimeout => stream.CanTimeout;

        public override int ReadTimeout { get => stream.ReadTimeout; set => stream.ReadTimeout = value; }

        public override long Position { get => stream.Position; set => stream.Position = value; }

        public override int WriteTimeout { get => stream.WriteTimeout; set => stream.WriteTimeout = value; }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = stream.Read(buffer, offset, count);
            bytesRead += n;
            lastUsed = DateTime.Now;
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
            bytesWritten += count;
            lastUsed = DateTime.Now;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                stream.Dispose();
            }
            base.Dispose(disposing);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return stream.FlushAsync(cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                var n = await stream.ReadAsync(buffer, offset, count, cancellationToken);
                bytesRead += n;
                lastUsed = DateTime.Now;
                return n;
            }
            catch (Exception) when (cancellationToken != null && cancellationToken.IsCancellationRequested)
            {
                // eat it.
                return 0;
            }
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                await stream.WriteAsync(buffer, offset, count, cancellationToken);
                bytesWritten += count;
                lastUsed = DateTime.Now;
            }
            catch (Exception) when (cancellationToken != null && cancellationToken.IsCancellationRequested)
            {
                // eat it.
            }
        }

        public override int ReadByte()
        {
            var n = stream.ReadByte();
            if (n > -1)
            {
                ++bytesRead;
            }
            lastUsed = DateTime.Now;
            return n;
        }

        public override void WriteByte(byte value)
        {
            stream.WriteByte(value);
            ++bytesWritten;
            lastUsed = DateTime.Now;
        }
    }
}
