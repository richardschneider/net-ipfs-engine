// BufferedStream is not available in Net Stardard 1.4
#if !NETSTANDARD14

// Part of JuiceStream: https://juicestream.machinezoo.com
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JuiceStream
{
    /// <summary>
    /// .NET already has its <c>BufferedStream</c>, but that one will throw unexpected exceptions, especially on <c>NetworkStreams</c>.
    /// JuiceStream's <c>DuplexBufferedStream</c> embeds two <c>BufferedStream</c> instances,
    /// one for each direction, to provide full duplex buffering over non-seekable streams.
    /// </summary>
    /// <remarks>
    ///   Copied from <see href="https://bitbucket.org/robertvazan/juicestream/raw/2caa975524900d1b5a76ddd3731c273d5dbb51eb/JuiceStream/DuplexBufferedStream.cs"/>
    /// </remarks>
    class DuplexBufferedStream : Stream
    {
        readonly Stream Inner;
        readonly BufferedStream ReadBuffer;
        readonly BufferedStream WriteBuffer;

        public override bool CanRead { get { return Inner.CanRead; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return Inner.CanWrite; } }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }

        public DuplexBufferedStream(Stream stream)
        {
            Inner = stream;
            ReadBuffer = new BufferedStream(stream);
            WriteBuffer = new BufferedStream(stream);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WriteBuffer.Flush();
                Inner.Dispose();
                ReadBuffer.Dispose();
                WriteBuffer.Dispose();
            }
        }

        public override void Flush() { WriteBuffer.Flush(); }
        public override Task FlushAsync(CancellationToken token) { return WriteBuffer.FlushAsync(token); }
        public override int Read(byte[] buffer, int offset, int count) { return ReadBuffer.Read(buffer, offset, count); }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) { return ReadBuffer.ReadAsync(buffer, offset, count, token); }
        public override int ReadByte() { return ReadBuffer.ReadByte(); }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { WriteBuffer.Write(buffer, offset, count); }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token) { return WriteBuffer.WriteAsync(buffer, offset, count, token); }
        public override void WriteByte(byte value) { WriteBuffer.WriteByte(value); }
    }
}

#endif