using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Ipfs.Engine.UnixFileSystem;

namespace Ipfs.Engine.CoreApi
{
    class ObjectApi : IObjectApi
    {
        internal static DagNode EmptyNode;
        internal static DagNode EmptyDirectory;

        IpfsEngine ipfs;

        static ObjectApi()
        {
            EmptyNode = new DagNode(new byte[0]);
            var _ = EmptyNode.Id;

            var dm = new DataMessage { Type = DataType.Directory };
            using (var pb = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize<DataMessage>(pb, dm);
                EmptyDirectory = new DagNode(pb.ToArray());
            }
            _ = EmptyDirectory.Id;
        }

        public ObjectApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<Stream> DataAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var node = await GetAsync(id, cancel);
            return node.DataStream;
        }

        public async Task<DagNode> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var block = await ipfs.Block.GetAsync(id, cancel);
            return new DagNode(block.DataStream);
        }

        public async Task<IEnumerable<IMerkleLink>> LinksAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var node = await GetAsync(id, cancel);
            return node.Links;
        }

        public Task<DagNode> NewAsync(string template = null, CancellationToken cancel = default(CancellationToken))
        {
            switch (template)
            {
                case null:
                    return Task.FromResult(EmptyNode);
                case "unixfs-dir":
                    return Task.FromResult(EmptyDirectory);
                default:
                    throw new ArgumentException($"Unknown template '{template}'.", "template");
            }
        }

        public Task<DagNode> NewDirectoryAsync(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(EmptyDirectory);
        }

        public Task<DagNode> PutAsync(byte[] data, IEnumerable<IMerkleLink> links = null, CancellationToken cancel = default(CancellationToken))
        {
            var node = new DagNode(data, links);
            return PutAsync(node, cancel);
        }

        public async Task<DagNode> PutAsync(DagNode node, CancellationToken cancel = default(CancellationToken))
        {
            node.Id = await ipfs.Block.PutAsync(node.ToArray(), cancel: cancel);
            return node;
        }
    }
}
