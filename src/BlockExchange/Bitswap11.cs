using Common.Logging;
using PeerTalk;
using PeerTalk.Protocols;
using ProtoBuf;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 0649 // disable warning about unassinged fields
#pragma warning disable 0169// disable warning about unassinged fields

namespace Ipfs.Engine.BlockExchange
{
    /// <summary>
    ///   Bitswap Protocol version 1.1.0 
    /// </summary>
    public class Bitswap11 : IBitswapProtocol
    {
        static ILog log = LogManager.GetLogger(typeof(Bitswap11));

        /// <inheritdoc />
        public string Name { get; } = "ipfs/bitswap";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1, 1);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"/{Name}/{Version}";
        }

        /// <summary>
        ///   The <see cref="Bitswap"/> service.
        /// </summary>
        public Bitswap Bitswap { get; set; }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection, Stream stream, CancellationToken cancel = default(CancellationToken))
        {
            // There is a race condition between getting the remote identity and
            // the remote sending the first wantlist.
            await connection.IdentityEstablished.Task.ConfigureAwait(false);

            while (true)
            {
                var request = await ProtoBufHelper.ReadMessageAsync<Message>(stream, cancel).ConfigureAwait(false);

                // Process want list
                if (request.wantlist != null && request.wantlist.entries != null)
                {
                    foreach (var entry in request.wantlist.entries)
                    {
                        var cid = Cid.Read(entry.block);
                        if (entry.cancel)
                        {
                            // TODO: Unwant specific to remote peer
                            Bitswap.Unwant(cid);
                        }
                        else
                        {
                            // TODO: Should we have a timeout?
                            var _ = GetBlockAsync(cid, connection.RemotePeer, CancellationToken.None);
                        }
                    }
                }

                // Forward sent blocks to the block service.  Eventually
                // bitswap will here about and them and then continue
                // any tasks (GetBlockAsync) waiting for the block.
                if (request.payload != null)
                {
                    log.Debug($"got block(s) from {connection.RemotePeer}");
                    foreach (var sentBlock in request.payload)
                    {
                        using (var ms = new MemoryStream(sentBlock.prefix))
                        {
                            var version = ms.ReadVarint32();
                            var contentType = ms.ReadMultiCodec().Name;
                            var multiHash = MultiHash.GetHashAlgorithmName(ms.ReadVarint32());
                            await Bitswap.OnBlockReceivedAsync(connection.RemotePeer, sentBlock.data, contentType, multiHash);
                        }
                    }
                }
            }
        }

        async Task GetBlockAsync(Cid cid, Peer remotePeer, CancellationToken cancel)
        {
            // TODO: Determine if we will fetch the block for the remote
            try
            {
                IDataBlock block;
                if (null != await Bitswap.BlockService.StatAsync(cid, cancel).ConfigureAwait(false))
                {
                    block = await Bitswap.BlockService.GetAsync(cid, cancel).ConfigureAwait(false);
                }
                else
                {
                    block = await Bitswap.WantAsync(cid, remotePeer.Id, cancel).ConfigureAwait(false);
                }

                // Send block to remote.
                using (var stream = await Bitswap.Swarm.DialAsync(remotePeer, this.ToString()).ConfigureAwait(false))
                {
                    await SendAsync(stream, block, cancel).ConfigureAwait(false);
                }
                await Bitswap.OnBlockSentAsync(remotePeer, block).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // eat it
            }
            catch (Exception e)
            {
                log.Warn("getting block for remote failed", e);
                // eat it.
            }
        }

        /// <inheritdoc />
        public async Task SendWantsAsync(
            Stream stream,
            IEnumerable<WantedBlock> wants,
            bool full = true,
            CancellationToken cancel = default(CancellationToken)
            )
        {
            var message = new Message
            {
                wantlist = new Wantlist
                {
                    full = full,
                    entries = wants
                        .Select(w => {
                            return new Entry
                            {
                                block = w.Id.ToArray()
                            };
                         })
                        .ToArray()
                },
                payload = new List<Block>(0)
            };

            ProtoBuf.Serializer.SerializeWithLengthPrefix<Message>(stream, message, PrefixStyle.Base128);
            await stream.FlushAsync(cancel).ConfigureAwait(false);
        }

        internal async Task SendAsync(
            Stream stream,
            IDataBlock block,
            CancellationToken cancel = default(CancellationToken)
            )
        {
            log.Debug($"Sending block {block.Id}");
            var message = new Message
            {
                payload = new List<Block>
                {
                    new Block
                    {
                        prefix =  GetCidPrefix(block.Id),
                        data = block.DataBytes
                    }
                }
            };

            ProtoBuf.Serializer.SerializeWithLengthPrefix<Message>(stream, message, PrefixStyle.Base128);
            await stream.FlushAsync(cancel).ConfigureAwait(false);
        }

        /// <summary>
        ///   Gets the CID "prefix".
        /// </summary>
        /// <param name="id">
        ///   The CID.
        /// </param>
        /// <returns>
        ///   A byte array of consisting of cid version, multicodec and multihash prefix (type + length).
        /// </returns>
        byte[] GetCidPrefix(Cid id)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteVarint(id.Version);
                ms.WriteMultiCodec(id.ContentType);
                ms.WriteVarint(id.Hash.Algorithm.Code);
                ms.WriteVarint(id.Hash.Digest.Length);
                return ms.ToArray();
            }
        }

        [ProtoContract]
        class Entry
        {
            [ProtoMember(1)]
            // changed from string to bytes, it makes a difference in JavaScript
            public byte[] block;      // the block cid (cidV0 in bitswap 1.0.0, cidV1 in bitswap 1.1.0)

            [ProtoMember(2)]
            public int priority = 1;    // the priority (normalized). default to 1

            [ProtoMember(3)]
            public bool cancel;       // whether this revokes an entry
        }

        [ProtoContract]
        class Wantlist
        {
            [ProtoMember(1)]
            public Entry[] entries;       // a list of wantlist entries

            [ProtoMember(2)]
            public bool full;           // whether this is the full wantlist. default to false
        }

        [ProtoContract]
        class Block
        {
            [ProtoMember(1)]
            public byte[] prefix;        // CID prefix (cid version, multicodec and multihash prefix (type + length)

            [ProtoMember(2)]
            public byte[] data;
        }

        [ProtoContract]
        class Message
        {
            [ProtoMember(1)]
            public Wantlist wantlist;

            [ProtoMember(2)]
            public byte[][] blocks;          // used to send Blocks in bitswap 1.0.0

            [ProtoMember(3)]
            public List<Block> payload;         // used to send Blocks in bitswap 1.1.0
        }

    }
}
