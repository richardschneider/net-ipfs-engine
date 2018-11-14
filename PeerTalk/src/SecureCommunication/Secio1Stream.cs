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
        static ILog log = LogManager.GetLogger(typeof(Secio1Stream));

        Stream stream;
        byte[] inBlock;
        int inBlockOffset;
        MemoryStream outStream = new MemoryStream();
        HMac inHmac;
        HMac outHmac;
        IBufferedCipher decrypt;
        IBufferedCipher encrypt;

        /// <summary>
        ///   Creates a new instance of the <see cref="Secio1Stream"/> class. 
        /// </summary>
        /// <param name="stream">
        ///   The source/destination  of SECIO packets.
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
                decrypt = CipherUtilities.GetCipher("AES/CTR/NOPADDING");
                var p = new ParametersWithIV(new KeyParameter(remoteKey.CipherKey), remoteKey.IV);
                decrypt.Init(false, p);

                encrypt = CipherUtilities.GetCipher("AES/CTR/PKCS7PADDING");
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
                    log.Debug("Need next packet");
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
            log.Debug($"Reading packet, length {length}");

            var encryptedData = await ReadPacketBytesAsync(length - outHmac.GetMacSize(), cancel);
            var signature = await ReadPacketBytesAsync(outHmac.GetMacSize(), cancel);
            log.Debug($"Reading encrypted, length {encryptedData.Length}");
            log.Debug($"Extra bytes = {encryptedData.Length % decrypt.GetBlockSize()}");

            var hmac = outHmac;
            var mac = new byte[hmac.GetMacSize()];
            hmac.Reset();
            hmac.BlockUpdate(encryptedData, 0, encryptedData.Length);
            hmac.DoFinal(mac, 0);
            if (!signature.SequenceEqual(mac))
                throw new InvalidDataException("HMac error");

            // Decrypt the data.
            //decrypt.Reset();
            var plainText = new byte[decrypt.GetOutputSize(encryptedData.Length)];
            var len = decrypt.ProcessBytes(encryptedData, 0, encryptedData.Length, plainText, 0);
            //decrypt.DoFinal(plainText, len);
            //var plainText = decrypt.ProcessBytes(encryptedData);

            var extraBytes = encryptedData.Length % decrypt.GetBlockSize();
            if (extraBytes > 0)
            {
                var padding = new byte[decrypt.GetBlockSize() - extraBytes];
                var x = decrypt.ProcessBytes(padding);
                Buffer.BlockCopy(x, 0, plainText, len, extraBytes);
            }

            log.Debug($"Read plain data {plainText.ToHexString()}");
            log.Debug($"Read plain text {Encoding.UTF8.GetString(plainText)}");
            return plainText;
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

            var plainData = outStream.ToArray();
            log.Debug($"Write plain data {plainData.ToHexString()}");
            log.Debug($"Write plain text {Encoding.UTF8.GetString(plainData)}");

            //encrypt.Reset();
            var encryptedData = encrypt.ProcessBytes(plainData);
            log.Debug($"Writer cipher length {encryptedData.Length}");

            var hmac = inHmac;
            var mac = new byte[hmac.GetMacSize()];
            hmac.Reset();
            hmac.BlockUpdate(encryptedData, 0, encryptedData.Length);
            hmac.DoFinal(mac, 0);

            var length = encryptedData.Length + mac.Length;
            stream.WriteByte((byte)(length >> 24));
            stream.WriteByte((byte)(length >> 16));
            stream.WriteByte((byte)(length >> 8));
            stream.WriteByte((byte)(length));
            stream.Write(encryptedData, 0, encryptedData.Length);
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

