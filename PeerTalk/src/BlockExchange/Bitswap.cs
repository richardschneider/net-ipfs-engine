using Common.Logging;
using Ipfs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.BlockExchange
{
    /// <summary>
    ///   Exchange blocks with other peers.
    /// </summary>
    public class Bitswap : IService
    {
        static ILog log = LogManager.GetLogger(typeof(Bitswap));

        ConcurrentDictionary<Cid, WantedBlock> wants = new ConcurrentDictionary<Cid, WantedBlock>();

        /// <inheritdoc />
        public Task StartAsync()
        {
            log.Debug("Starting");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            log.Debug("Stopping");

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
            var tsc = new TaskCompletionSource<IDataBlock>();
            wants.AddOrUpdate(
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

            // TODO: Tell other peers

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
    }
}
