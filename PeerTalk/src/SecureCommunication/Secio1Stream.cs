using Ipfs;
using Common.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using PeerTalk.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Engines;

namespace PeerTalk.SecureCommunication
{
    /// <summary>
    ///   A duplex stream that is encrypted and signed.
    /// </summary>
    /// <remarks>
    ///   A packet consists of a [uint32 length of packet | encrypted body | hmac signature of encrypted body].
    ///   <para>
    ///   Writing data is buffered until <see cref="FlushAsync(CancellationToken)"/> is
    ///   called.
    ///   </para>
    /// </remarks>
    public class Secio1Stream : Stream
    {
        Stream stream;
        byte[] inBlock;
        int inBlockOffset;
        MemoryStream outStream = new MemoryStream();
        HMac inHmac;
        HMac outHmac;
        IStreamCipher decrypt;
        IStreamCipher encrypt;

        /// <summary>
        ///   Creates a new instance of the <see cref="Secio1Stream"/> class. 
        /// </summary>
        /// <param name="stream">
        ///   The source/destination of SECIO packets.
        /// </param>
        /// <param name="cipherName">
        ///   The cipher for the <paramref name="stream"/>, such as AES-256 or AES-128.
        /// </param>
        /// <param name="hashName">
        ///   The hash for the <paramref name="stream"/>, such as SHA256.
        /// </param>
        /// <param name="localKey">
        ///   The keys used by the local endpoint.
        /// </param>
        /// <param name="remoteKey">
        ///   The keys used by the remote endpoint.
        /// </param>
        public Secio1Stream(
            Stream stream, 
            string cipherName, string hashName, 
            StretchedKey localKey, StretchedKey remoteKey)
        {
            this.stream = stream;

            inHmac = new HMac(DigestUtilities.GetDigest(hashName));
            inHmac.Init(new KeyParameter(localKey.MacKey));

            outHmac = new HMac(DigestUtilities.GetDigest(hashName));
            outHmac.Init(new KeyParameter(remoteKey.MacKey));

            if (cipherName == "AES-256" || cipherName == "AES-512")
            {
                decrypt = new CtrStreamCipher(new AesEngine());
                var p = new ParametersWithIV(new KeyParameter(remoteKey.CipherKey), remoteKey.IV);
                decrypt.Init(false, p);

                encrypt = new CtrStreamCipher(new AesEngine());
                p = new ParametersWithIV(new KeyParameter(localKey.CipherKey), localKey.IV);
                encrypt.Init(true, p);
            }
            else
            {
                throw new NotSupportedException($"Cipher '{cipherName}' is not supported.");
            }
        }

        /// <inheritdoc />
        public override bool CanRead => stream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => stream.CanRead;

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
        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).Result;
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int total = 0;
            while (count > 0)
            {
                // Does the current packet have some unread data?
                if (inBlock != null && inBlockOffset < inBlock.Length)
                {
                    var n = Math.Min(inBlock.Length - inBlockOffset, count);
                    Array.Copy(inBlock, inBlockOffset, buffer, offset, n);
                    total += n;
                    count -= n;
                    offset += n;
                    inBlockOffset += n;
                }
                // Otherwise, wait for a new block of data.
                else
                {
                    inBlock = await ReadPacketAsync(cancellationToken);
                    inBlockOffset = 0;
                }
            }

            return total;
        }

        /// <summary>
        ///   Read an encrypted and signed packet.
        /// </summary>
        /// <returns>
        ///   The plain text as an array of bytes.
        /// </returns>
        /// <remarks>
        ///   A packet consists of a [uint32 length of packet | encrypted body | hmac signature of encrypted body].
        /// </remarks>
        async Task<byte[]> ReadPacketAsync(CancellationToken cancel)
        {
            var lengthBuffer = await ReadPacketBytesAsync(4, cancel);
            var length =
                (int)lengthBuffer[0] << 24 |
                (int)lengthBuffer[1] << 16 |
                (int)lengthBuffer[2] << 8 |
                (int)lengthBuffer[3];
            if (length <= outHmac.GetMacSize())
                throw new InvalidDataException($"Invalid secio packet length of {length}.");

            var encryptedData = await ReadPacketBytesAsync(length - outHmac.GetMacSize(), cancel);
            var signature = await ReadPacketBytesAsync(outHmac.GetMacSize(), cancel);

            var hmac = outHmac;
            var mac = new byte[hmac.GetMacSize()];
            hmac.Reset();
            hmac.BlockUpdate(encryptedData, 0, encryptedData.Length);
            hmac.DoFinal(mac, 0);
            if (!signature.SequenceEqual(mac))
                throw new InvalidDataException("HMac error");

            // Decrypt the data in-place.
            decrypt.ProcessBytes(encryptedData, 0, encryptedData.Length, encryptedData, 0);
            return encryptedData;
        }

        async Task<byte[]> ReadPacketBytesAsync(int count, CancellationToken cancel)
        {
            byte[] buffer = new byte[count];
            for (int i = 0, n; i < count; i += n)
            {
                n = await stream.ReadAsync(buffer, i, count - i, cancel);
                if (n < 1)
                    throw new EndOfStreamException();
            }
            return buffer;
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

            var data = outStream.ToArray();  // plain text
            encrypt.ProcessBytes(data, 0, data.Length, data, 0);

            var hmac = inHmac;
            var mac = new byte[hmac.GetMacSize()];
            hmac.Reset();
            hmac.BlockUpdate(data, 0, data.Length);
            hmac.DoFinal(mac, 0);

            var length = data.Length + mac.Length;
            stream.WriteByte((byte)(length >> 24));
            stream.WriteByte((byte)(length >> 16));
            stream.WriteByte((byte)(length >> 8));
            stream.WriteByte((byte)(length));
            stream.Write(data, 0, data.Length);
            stream.Write(mac, 0, mac.Length);
            await stream.FlushAsync(cancel);

            outStream.SetLength(0);
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

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                stream.Dispose();
            }
            base.Dispose(disposing);
        }

    }

}

