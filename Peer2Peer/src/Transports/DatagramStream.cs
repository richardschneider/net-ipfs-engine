using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer.Transports
{
    class DatagramStream : Stream
    {
        Socket socket;
        bool ownsSocket;
        MemoryStream sendBuffer = new MemoryStream();
        MemoryStream receiveBuffer = new MemoryStream();
        byte[] datagram = new byte[2048];

        public DatagramStream(Socket socket, bool ownsSocket = false)
        {
            this.socket = socket;
            this.ownsSocket = ownsSocket;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Flush();
                }
                catch (SocketException)
                {
                    // eat it
                }
            }
            if (ownsSocket && socket != null)
            {
                try
                {
                    socket.Dispose();
                }
                catch (SocketException)
                {
                    // eat it
                }
                finally
                {
                    socket = null;
                }
            }
            base.Dispose(disposing);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
            FlushAsync().Wait();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (sendBuffer.Position > 0)
            {
                var bytes = new ArraySegment<byte>(sendBuffer.ToArray());
                sendBuffer.Position = 0;
                await socket.SendAsync(bytes, SocketFlags.None);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).Result;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // If no data.
            if (receiveBuffer.Position == receiveBuffer.Length)
            {
                await FlushAsync();
                receiveBuffer.Position = 0;
                receiveBuffer.SetLength(0);
                var size = socket.Receive(datagram);
                receiveBuffer.Write(datagram, 0, size);
                receiveBuffer.Position = 0;
            }
            return receiveBuffer.Read(buffer, offset, count);
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
            sendBuffer.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }
    }
}
