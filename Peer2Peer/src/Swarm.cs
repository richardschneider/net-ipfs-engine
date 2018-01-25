using Ipfs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace Peer2Peer
{
    /// <summary>
    ///   Manages communication with other peers.
    /// </summary>
    public class Swarm : IService, IPolicy<MultiAddress>
    {
        /// <summary>
        ///   Other nodes.
        /// </summary>
        ConcurrentBag<MultiAddress> others = new ConcurrentBag<MultiAddress>();

        /// <summary>
        ///   Get the sequence of all known peer addresses.
        /// </summary>
        /// <value>
        ///   Contains any peer address that has been
        ///   <see cref="RegisterPeerAsync">discovered</see>.
        /// </value>
        /// <seealso cref="RegisterPeerAsync"/>
        public IEnumerable<MultiAddress> KnownPeerAddresses
        {
            get { return others; }
        }

        /// <summary>
        ///   Register that a peer's address has been discovered.
        /// </summary>
        /// <param name="address">
        ///   An address to the peer. 
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous get operation. The task's result
        ///    is <b>true</b> if the <paramref name="address"/> is registered.
        /// </returns>
        /// <remarks>
        ///   If the <paramref name="address"/> is not already known, then it is
        ///   added to the <see cref="KnownPeerAddresses"/> unless the <see cref="BlackList"/>
        ///   or <see cref="WhiteList"/> policies forbid it.
        /// </remarks>
        public async Task<bool> RegisterPeerAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            if (await IsAllowedAsync(address, cancel) && !others.Contains(address))
            {
                others.Add(address);
                return true;
            }

            return false;
        }

        /// <summary>
        ///   The addresses that cannot be used.
        /// </summary>
        public BlackList<MultiAddress> BlackList { get; set;  } = new BlackList<MultiAddress>();

        /// <summary>
        ///   The addresses that can be used.
        /// </summary>
        public WhiteList<MultiAddress> WhiteList { get; set;  } = new WhiteList<MultiAddress>();

        /// <inheritdoc />
        public Task StartAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<bool> IsAllowedAsync(MultiAddress target, CancellationToken cancel = default(CancellationToken))
        {
            return await BlackList.IsAllowedAsync(target, cancel)
                && await WhiteList.IsAllowedAsync(target, cancel);
        }

        /// <inheritdoc />
        public async Task<bool> IsNotAllowedAsync(MultiAddress target, CancellationToken cancel = default(CancellationToken))
        {
            var q = await IsAllowedAsync(target, cancel);
            return !q;
        }
    }
}
