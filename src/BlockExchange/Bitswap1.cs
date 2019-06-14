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
    ///   Bitswap Protocol version 1.0.0 
    /// </summary>
    public class Bitswap1 : IBitswapProtocol
    {
        static ILog log = LogManager.GetLogger(typeof(Bitswap1));

        /// <inheritdoc />
        public string Name { get; } = "ipfs/bitswap";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1, 0);

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
            var request = await ProtoBufHelper.ReadMessageAsync<Message>(stream, cancel).ConfigureAwait(false);

            // There is a race condition between getting the remote identity and
            // the remote sending the first wantlist.
            await connection.IdentityEstablished.Task.ConfigureAwait(false);

            log.Debug($"got message from {connection.RemotePeer}");

            // Process want list
            if (request.wantlist != null && request.wantlist.entries != null)
            {
                log.Debug("got want list");
                foreach (var entry in request.wantlist.entries)
                {
                    var s = Base58.ToBase58(entry.block);
                    Cid cid = s;
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
            if (request.blocks != null)
            {
                log.Debug("got some blocks");
                foreach (var sentBlock in request.blocks)
                {
                    ++Bitswap.BlocksReceived;
                    Bitswap.DataReceived += (ulong)sentBlock.Length;
                    await Bitswap.BlockService.PutAsync(sentBlock, pin: false).ConfigureAwait(false);
                    // TODO: Detect if duplicate and update stats
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
            log.Debug("Sending want list");

            var message = new Message
            {
                wantlist = new Wantlist
                {
                    full = full,
                    entries = wants
                        .Select(w => new Entry
                        {
                            block = w.Id.Hash.ToArray()
                        })
                        .ToArray()
                }
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
            ++Bitswap.BlocksSent;
            Bitswap.DataSent += (ulong)block.Size;

            var message = new Message
            {
                blocks = new byte[][]
                {
                    block.DataBytes
                }
            };

            ProtoBuf.Serializer.SerializeWithLengthPrefix<Message>(stream, message, PrefixStyle.Base128);
            await stream.FlushAsync(cancel).ConfigureAwait(false);
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
        class Message
        {
            [ProtoMember(1)]
            public Wantlist wantlist;

            [ProtoMember(2)]
            public byte[][] blocks;
        }

    }
}
