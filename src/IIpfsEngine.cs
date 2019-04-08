using System;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Ipfs.Engine.Cryptography;
using Nito.AsyncEx;
using PeerTalk;

namespace Ipfs.Engine
{
    /// <summary>
    ///   Offers an abstraction of the IpfsEngine class for consumers of the library.
    /// </summary>
    public interface IIpfsEngine : ICoreApi, IService, IDisposable
    {
        /// <summary>
        ///   The configuration options.
        /// </summary>
        IpfsEngineOptions Options { get; set; }

        /// <inheritdoc />
        IBitswapApi Bitswap { get; }

        /// <inheritdoc />
        IBlockApi Block { get; }

        /// <inheritdoc />
        IBootstrapApi Bootstrap { get; }

        /// <inheritdoc />
        IConfigApi Config { get; }

        /// <inheritdoc />
        IDagApi Dag { get; }

        /// <inheritdoc />
        IDhtApi Dht { get; }

        /// <inheritdoc />
        IDnsApi Dns { get; }

        /// <inheritdoc />
        IFileSystemApi FileSystem { get; }

        /// <inheritdoc />
        IGenericApi Generic { get; }

        /// <inheritdoc />
        IKeyApi Key { get; }

        /// <inheritdoc />
        INameApi Name { get; }

        /// <inheritdoc />
        IObjectApi Object { get; }

        /// <inheritdoc />
        IPinApi Pin { get; }

        /// <inheritdoc />
        IPubSubApi PubSub { get; }

        /// <inheritdoc />
        ISwarmApi Swarm { get; }

        /// <inheritdoc />
        IStatsApi Stats { get; }

        /// <summary>
        ///   Provides access to the local peer.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a <see cref="Peer"/>.
        /// </returns>
        AsyncLazy<Peer> LocalPeer { get; }

        /// <summary>
        ///   Manages communication with other peers.
        /// </summary>
        AsyncLazy<Swarm> SwarmService { get; }

        /// <summary>
        ///   Exchange blocks with other peers.
        /// </summary>
        AsyncLazy<BlockExchange.Bitswap> BitswapService { get; }

        /// <summary>
        ///   Finds information with a distributed hash table.
        /// </summary>
        AsyncLazy<PeerTalk.Routing.Dht1> DhtService { get; }

        /// <summary>
        ///   Provides access to the <see cref="KeyChain"/>.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the <see cref="keyChain"/>.
        /// </returns>
        Task<KeyChain> KeyChain(CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Resolve an "IPFS path" to a content ID.
        /// </summary>
        /// <param name="path">
        ///   A IPFS path, such as "Qm...", "Qm.../a/b/c" or "/ipfs/QM..."
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   The content ID of <paramref name="path"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   The <paramref name="path"/> cannot be resolved.
        /// </exception>
        Task<Cid> ResolveIpfsPathToCidAsync (string path, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Starts the network services.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   Starts the various IPFS and PeerTalk network services.  This should
        ///   be called after any configuration changes.
        /// </remarks>
        /// <exception cref="Exception">
        ///   When the engine is already started.
        /// </exception>
        Task StartAsync();

        /// <summary>
        ///   Stops the running services.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   Multiple calls are okay.
        /// </remarks>
        Task StopAsync();

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        ///   Waits for <see cref="StopAsync"/> to complete.
        /// </remarks>
        void Dispose();
    }
}