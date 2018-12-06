using Common.Logging;
using Ipfs.CoreApi;
using PeerTalk;
using PeerTalk.Protocols;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine.BlockExchange
{
    /// <summary>
    ///   Exchange blocks with other peers.
    /// </summary>
    public class Bitswap : IService
    {
        static ILog log = LogManager.GetLogger(typeof(Bitswap));

        ConcurrentDictionary<Cid, WantedBlock> wants = new ConcurrentDictionary<Cid, WantedBlock>();
        IBitswapProtocol[] protocols;

        /// <summary>
        ///   Creates a new instance of the <see cref="Bitswap"/> class.
        /// </summary>
        public Bitswap()
        {
            protocols = new IBitswapProtocol[]
            {
                new Bitswap11 { Bitswap = this },
                new Bitswap1 { Bitswap = this }
            };
        }

        /// <summary>
        ///   Provides access to other peers.
        /// </summary>
        public Swarm Swarm { get; set; }

        /// <summary>
        ///   Provides access to blocks of data.
        /// </summary>
        public IBlockApi BlockService { get; set; }


        /// <summary>
        ///   Raised when a blocked is needed.
        /// </summary>
        /// <remarks>
        ///   Only raised when a block is first requested.
        /// </remarks>
        public event EventHandler<CidEventArgs> BlockNeeded;

        /// <inheritdoc />
        public Task StartAsync()
        {
            log.Debug("Starting");

            foreach (var protocol in protocols)
            {
                Swarm.AddProtocol(protocol);
            }
            Swarm.ConnectionEstablished += Swarm_ConnectionEstablished;

            return Task.CompletedTask;
        }

        // When a connection is established
        // (1) Send the local peer's want list to the remote
        async void Swarm_ConnectionEstablished(object sender, PeerConnection connection)
        {
            try
            {
                // There is a race condition between getting the remote identity and
                // the remote sending the first wantlist.
                await connection.IdentityEstablished.Task;

                // Fire and forget.
                var _ = SendWantListAsync(connection.RemotePeer);
            }
            catch (Exception e)
            {
                log.Warn("Sending want list", e);
            }
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            log.Debug("Stopping");

            Swarm.ConnectionEstablished -= Swarm_ConnectionEstablished;
            foreach (var protocol in protocols)
            {
                Swarm.RemoveProtocol(protocol);
            }

            foreach (var cid in wants.Keys)
            {
                Unwant(cid);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///   The blocks needed by the peer.
        /// </summary>
        /// <param name="peer">
        ///   The unique ID of the peer.
        /// </param>
        /// <returns>
        ///   The sequence of CIDs need by the <paramref name="peer"/>.
        /// </returns>
        public IEnumerable<Cid> PeerWants(MultiHash peer)
        {
            return wants.Values
                .Where(w => w.Peers.Contains(peer))
                .Select(w => w.Id);
        }

        /// <summary>
        ///   Adds a block to the want list.
        /// </summary>
        /// <param name="id">
        ///   The CID of the block to add to the want list.
        /// </param>
        /// <param name="peer">
        ///   The unique ID of the peer that wants the block.  This is for
        ///   information purposes only.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the contents of block.
        /// </returns>
        /// <remarks>
        ///   Other peers are informed that the block is needed by this peer. Hopefully,
        ///   someone will forward it to us.
        ///   <para>
        ///   Besides using <paramref name="cancel"/> for cancellation, the 
        ///   <see cref="Unwant"/> method will also cancel the operation.
        ///   </para>
        /// </remarks>
        public Task<IDataBlock> Want(Cid id, MultiHash peer, CancellationToken cancel)
        {
            log.Trace($"{peer} wants {id}");

            var tsc = new TaskCompletionSource<IDataBlock>();
            var want = wants.AddOrUpdate(
                id,
                (key) => new WantedBlock
                {
                    Id = id,
                    Consumers = new List<TaskCompletionSource<IDataBlock>> { tsc },
                    Peers = new List<MultiHash> { peer }
                },
                (key, block) =>
                {
                    block.Peers.Add(peer);
                    block.Consumers.Add(tsc);
                    return block;
                }
            );

            // If cancelled, then the block is unwanted.
            cancel.Register(() => Unwant(id));

            // If first time, tell other peers.
            if (want.Consumers.Count == 1)
            {
                BlockNeeded?.Invoke(this, new CidEventArgs { Id = want.Id });
            }
            if (peer == Swarm.LocalPeer.Id)
            {
                // Fire and forget.
                var _ = SendWantListToAllAsync(new[] { want }, full: false);
            }

            return tsc.Task;
        }

        /// <summary>
        ///   Removes the block from the want list.
        /// </summary>
        /// <param name="id">
        ///   The CID of the block to remove from the want list.
        /// </param>
        /// <remarks>
        ///   Any tasks waiting for the block are cancelled.
        ///   <para>
        ///   No exception is thrown if the <paramref name="id"/> is not
        ///   on the want list.
        ///   </para>
        /// </remarks>
        public void Unwant(Cid id)
        {
            if (wants.TryRemove(id, out WantedBlock block))
            {
                foreach (var consumer in block.Consumers)
                {
                    consumer.SetCanceled();
                }
            }
        }

        /// <summary>
        ///   Indicate that a block is found.
        /// </summary>
        /// <param name="block">
        ///   The block that was found.
        /// </param>
        /// <returns>
        ///   The number of consumers waiting for the <paramref name="block"/>.
        /// </returns>
        /// <remarks>
        ///   <b>Found</b> should be called whenever a new block is discovered. 
        ///   It will continue any Task that is waiting for the block and
        ///   remove the block from the want list.
        /// </remarks>
        public int Found(IDataBlock block)
        {
            if (wants.TryRemove(block.Id, out WantedBlock want))
            {
                foreach (var consumer in want.Consumers)
                {
                    consumer.SetResult(block);
                }
                return want.Consumers.Count;
            }

            return 0;
        }

        Task SendWantListAsync(Peer peer)
        {
            var myWants = PeerWants(Swarm.LocalPeer.Id);
            if (myWants.Count() > 0)
            {
                return SendWantListAsync(peer, wants.Values, true);
            }

            return Task.CompletedTask;

        }

        /// <summary>
        ///   Send our want list to the connected peers.
        /// </summary>
        Task SendWantListToAllAsync(IEnumerable<WantedBlock> wants, bool full)
        {
            log.Debug("Spamming all connected peers");
            if (Swarm == null)
                return Task.CompletedTask;

            var tasks = Swarm.KnownPeers
                .Where(p => p.ConnectedAddress != null)
                .Select(p => SendWantListAsync(p, wants, full));
            return Task.WhenAll(tasks);
        }

        async Task SendWantListAsync(Peer peer, IEnumerable<WantedBlock> wants, bool full)
        {
            log.Debug($"sending want list to {peer}");

            // Send the want list to the peer on any bitswap protocol
            // that it supports.
            foreach (var protocol in protocols)
            {
                try
                {
                    using (var stream = await Swarm.DialAsync(peer, protocol.ToString()))
                    {
                        await protocol.SendWantsAsync(stream, wants, full: full);
                    }
                    return;
                }
                catch (Exception e)
                {
                    log.Debug($"{peer} refused {protocol}", e);
                }
            }

            log.Warn($"{peer} does not support any bitswap protocol");
        }

    }
}
