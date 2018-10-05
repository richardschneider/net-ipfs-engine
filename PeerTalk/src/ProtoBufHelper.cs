using Ipfs;
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
    ///   Helper methods for ProtoBuf.
    /// </summary>
    public static class ProtoBufHelper
    {
        /// <summary>
        ///   Read a proto buf message.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of message.
        /// </typeparam>
        /// <param name="stream">
        ///   The stream containing the message.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the <typeparamref name="T"/> message.
        /// </returns>
        public static async Task<T> ReadMessageAsync<T>(Stream stream, CancellationToken cancel = default(CancellationToken))
        {
            var length = await stream.ReadVarint32Async(cancel);
            var bytes = new byte[length];
            for (int offset = 0; offset < length;) {
                offset += await stream.ReadAsync(bytes, offset, length - offset);
            }

            using (var ms = new MemoryStream(bytes, false))
            {
                return ProtoBuf.Serializer.Deserialize<T>(ms);
            }
        }
    }
}
