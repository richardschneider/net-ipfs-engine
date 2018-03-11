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
    ///   Provides read-only access to a chunked file.
    /// </summary>
    /// <remarks>
    ///   Internal class to support <see cref="FileSystem"/>.
    /// </remarks>
    public class ChunkedStream : Stream
    {
        class BlockInfo
        {
            public Cid Id;
            public long Position;
        }

        List<BlockInfo> blocks = new List<BlockInfo>();
        long fileSize;

        /// <summary>
        ///   Creates a new instance of the <see cref="ChunkedStream"/> class with
        ///   the specified <see cref="IBlockApi"/> and <see cref="DagNode"/>.
        /// </summary>
        /// <param name="blockService"></param>
        /// <param name="dag"></param>
        public ChunkedStream (IBlockApi blockService, DagNode dag)
        {
            BlockService = blockService;
            var links = dag.Links.ToArray();
            var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);
            fileSize = (long)dm.FileSize;
            ulong position = 0;
            for (int i = 0; i < dm.BlockSizes.Length; ++i)
            {
                blocks.Add(new BlockInfo
                {
                    Id = links[i].Id,
                    Position = (long) position
                });
                position += dm.BlockSizes[i];
            }
        }

        IBlockApi BlockService { get; set; }

        /// <inheritdoc />
        public override long Length => fileSize;

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => true;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override void Flush() { }

        /// <inheritdoc />
        public override long Position { get; set; }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: Position = offset; break;
                case SeekOrigin.Current: Position += offset; break;
                case SeekOrigin.End: Position = Length - offset; break;
            }
            return Position;
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).Result;
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancel)
        {
            ICollection<byte> block = await GetBlockAsync(Position, count, cancel);
            if (block.Count > 0)
            {
                block.CopyTo(buffer, offset);
                Position += block.Count;
            }
            return block.Count;
        }

        BlockInfo currentBlock;
        byte[] currentData;
        async Task<ArraySegment<byte>> GetBlockAsync (long position, int count, CancellationToken cancel)
        {
            if (position >= Length)
            {
                return new ArraySegment<byte>();
            }
            var need = blocks.Last(b => b.Position <= position);
            if (need != currentBlock)
            {
                var block = await BlockService.GetAsync(need.Id, cancel);
                currentBlock = need;
                var dag = new DagNode(block.DataStream);
                var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);
                currentData = dm.Data;
            }
            return new ArraySegment<byte>(currentData, (int)(Position - currentBlock.Position), currentData.Length);
        }
    }
}
