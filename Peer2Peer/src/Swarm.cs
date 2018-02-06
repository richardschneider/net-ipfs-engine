using Ipfs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using Common.Logging;

namespace Peer2Peer
{
    /// <summary>
    ///   Manages communication with other peers.
    /// </summary>
    public class Swarm : IService, IPolicy<MultiAddress>
    {
        static ILog log = LogManager.GetLogger(typeof(Swarm));

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
        ///   Get the sequence of all known peers.
        /// </summary>
        /// <value>
        ///   Contains any peer that has been
        ///   <see cref="RegisterPeerAsync">discovered</see>.
        /// </value>
        /// <seealso cref="RegisterPeerAsync"/>
        public IEnumerable<Peer> KnownPeers
        {
            get
            {
                return KnownPeerAddresses.GroupBy(
                    a => a.Protocols.Last().Value,
                    a => a,
                    (k, v) => new Peer
                    {
                        Id = k,
                        Addresses = v
                    }
                );
            }
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
        ///    A task that represents the asynchronous operation. The task's result
        ///    is <b>true</b> if the <paramref name="address"/> is registered.
        /// </returns>
        /// <remarks>
        ///   If the <paramref name="address"/> is not already known, then it is
        ///   added to the <see cref="KnownPeerAddresses"/> unless the <see cref="BlackList"/>
        ///   or <see cref="WhiteList"/> policies forbid it.
        /// </remarks>
        public async Task<bool> RegisterPeerAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            if (address.Protocols.Last().Name != "ipfs")
            {
                log.ErrorFormat("'{0}' missing ipfs protocol name", address);
                return false;
            }

            if (others.Contains(address))
            {
                log.DebugFormat("Already registered {0}", address);
                return false;
            }

            if (await IsAllowedAsync(address, cancel))
            {
                others.Add(address);
                log.DebugFormat("Registered {0}", address);
                return true;
            }

            log.WarnFormat("Not allowed {0}", address);
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
            log.Debug("Starting");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            log.Debug("Stopping");

            others = new ConcurrentBag<MultiAddress>();
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
