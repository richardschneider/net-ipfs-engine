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
    static class ProtoBufHelper
    {
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
