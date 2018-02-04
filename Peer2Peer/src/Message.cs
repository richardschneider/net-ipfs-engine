using Common.Logging;
using Ipfs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peer2Peer
{
    /// <summary>
    ///   A message that is exchanged between peers.
    /// </summary>
    /// <remarks>
    ///   A message consists of
    ///   <list type="bullet">
    ///      <item><description>A <see cref="Varint"/> length prefix</description></item>
    ///      <item><description>The payload</description></item>
    ///      <item><description>A terminating newline</description></item>
    ///   </list>
    /// </remarks>
    public static class Message
    {
        static byte[] newline = new byte[] { 0x0a };
        static ILog log = LogManager.GetLogger(typeof(Message));

        /// <summary>
        ///   Read the message as a sequence of bytes from the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The <see cref="Stream"/> to a peer.
        /// </param>
        /// <returns>
        ///   The byte representation of the message's payload.
        /// </returns>
        /// <exception cref="InvalidDataException">
        ///   When the message is invalid.
        /// </exception>
        /// <remarks>
        ///   The return value has the length prefix and terminating newline removed.
        /// </remarks>
        public static byte[] ReadBytes(Stream stream)
        {
            var length = stream.ReadVarint32();
            var buffer = new byte[length - 1];
            stream.Read(buffer, 0, length - 1);
            if (stream.ReadByte() != newline[0])
            {
                throw new InvalidDataException("Missing terminating newline");
            }
            return buffer;
        }

        /// <summary>
        ///   Read the message as a <see cref="string"/> from the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The <see cref="Stream"/> to a peer.
        /// </param>
        /// <returns>
        ///   The string representation of the message's payload.
        /// </returns>
        /// <exception cref="InvalidDataException">
        ///   When the message is invalid.
        /// </exception>
        /// <remarks>
        ///   The return value has the length prefix and terminating newline removed.
        /// </remarks>
        public static string ReadString(Stream stream)
        {
            var payload = Encoding.UTF8.GetString(ReadBytes(stream));

            log.Debug("received " + payload);
            return payload;
        }

        /// <summary>
        ///   Writes the binary representation of the message to the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="message">
        ///   The message to write.  A newline is automatically appended.
        /// </param>
        /// <param name="stream">
        ///   The <see cref="Stream"/> to a peer.
        /// </param>
        public static void Write(string message, Stream stream)
        {
            log.Debug("sending " + message);

            var payload = Encoding.UTF8.GetBytes(message);
            stream.WriteVarint(message.Length + 1);
            stream.Write(payload, 0, payload.Length);
            stream.Write(newline, 0, newline.Length);
            stream.Flush();
        }
    }
}
