using Ipfs.CoreApi;
using ProtoBuf;
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
    ///   Support for the *nix file system.
    /// </summary>
    public static class FileSystem
    {
        static byte[] emptyData = new byte[0];

        /// <summary>
        ///   Creates a stream that can read the supplied <see cref="Cid"/>.
        /// </summary>
        /// <param name="id">
        ///   The identifier of some content.
        /// </param>
        /// <param name="blockService">
        ///   The source of cid's data.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   a <see cref="Stream"/> that produces the content of the <paramref name="id"/>.
        /// </returns>
        /// <remarks>
        ///  The id's <see cref="Cid.ContentType"/> is used to determine how to read
        ///  the conent.
        /// </remarks>
        public static async Task<Stream> CreateReadStream(
            Cid id,
            IBlockApi blockService,
            CancellationToken cancel)
        {
            // TODO: A content-type registry should be used.
            if (id.ContentType == "dag-pb")
                return await CreateDagProtoBufStreamAsync(id, blockService, cancel);
            else if (id.ContentType == "raw")
                return await CreateRawStreamAsync(id, blockService, cancel);
            else
                throw new NotSupportedException($"Cannot read content type '{id.ContentType}'.");
        }

        static async Task<Stream> CreateRawStreamAsync(
            Cid id,
            IBlockApi blockService,
            CancellationToken cancel)
        {
            var block = await blockService.GetAsync(id, cancel);
            return block.DataStream;
        }

        static async Task<Stream> CreateDagProtoBufStreamAsync(
            Cid id,
            IBlockApi blockService,
            CancellationToken cancel)
        {
            var block = await blockService.GetAsync(id, cancel);
            var dag = new DagNode(block.DataStream);
            var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);

            if (dm.Type != DataType.File)
                throw new Exception($"'{id.Encode()}' is not a file.");

            if (dm.Fanout.HasValue) throw new NotImplementedException("files with a fanout");

            // Is it a simple node?
            if (dm.BlockSizes == null && !dm.Fanout.HasValue)
            {
                return new MemoryStream(buffer: dm.Data ?? emptyData, writable: false);
            }

            if (dm.BlockSizes != null)
            {
                return new ChunkedStream(blockService, dag);
            }

            throw new Exception($"Cannot determine the file format of '{id}'.");
        }
    }
}
