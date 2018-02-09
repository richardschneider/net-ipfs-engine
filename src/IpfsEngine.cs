using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Ipfs.CoreApi;
using Ipfs.Engine.CoreApi;
using Ipfs.Engine.Cryptography;
using Peer2Peer;
using System.Reflection;
using Peer2Peer.Discovery;
using Nito.AsyncEx;

namespace Ipfs.Engine
{
    /// <summary>
    ///   TODO
    /// </summary>
    public partial class IpfsEngine : ICoreApi, IService, IDisposable
    {
        static ILog log = LogManager.GetLogger(typeof(IpfsEngine));

        bool repositoryInited;
        KeyChain keyChain;
        char[] passphrase;
        List<Task> stopTasks = new List<Task>();

        /// <summary>
        ///   Creates a new instance of the <see cref="IpfsEngine"/> class.
        /// </summary>
        public IpfsEngine(char[] passphrase)
        {
            this.passphrase = passphrase;

            // Init the core api inteface.
            Bitswap = new BitswapApi(this);
            Block = new BlockApi(this);
            Bootstrap = new BootstrapApi(this);
            Config = new ConfigApi(this);
            Dag = new DagApi(this);
            Dht = new DhtApi(this);
            FileSystem = new FileSystemApi(this);
            Generic = new GenericApi(this);
            Key = new KeyApi(this);
            Name = new NameApi(this);
            Object = new ObjectApi(this);
            Pin = new PinApi(this);
            PubSub = new PubSubApi(this);
            Swarm = new SwarmApi(this);

            // Async properties
            LocalPeer = new AsyncLazy<Peer>(async () =>
            {
                log.Debug("Building local peer");
                var keyChain = await KeyChain();
                var self = await keyChain.FindKeyByNameAsync("self");
                var localPeer = new Peer();
                localPeer.Id = self.Id;
                localPeer.PublicKey = await keyChain.GetPublicKeyAsync("self");
                localPeer.ProtocolVersion = "ipfs/0.1.0";
                var version = typeof(IpfsEngine).GetTypeInfo().Assembly.GetName().Version;
                localPeer.AgentVersion = $"net-ipfs/{version.Major}.{version.Minor}.{version.Revision}";
                return localPeer;
            });
        }

        /// <summary>
        ///   The configuration options.
        /// </summary>
        public IpfsEngineOptions Options { get; set; } = new IpfsEngineOptions();

        /// <inheritdoc />
        public IBitswapApi Bitswap { get; private set; }

        /// <inheritdoc />
        public IBlockApi Block { get; private set; }

        /// <inheritdoc />
        public IBootstrapApi Bootstrap { get; private set; }

        /// <inheritdoc />
        public IConfigApi Config { get; private set; }

        /// <inheritdoc />
        public IDagApi Dag { get; private set; }

        /// <inheritdoc />
        public IDhtApi Dht { get; private set; }

        /// <inheritdoc />
        public IFileSystemApi FileSystem { get; private set; }

        /// <inheritdoc />
        public IGenericApi Generic { get; private set; }

        /// <inheritdoc />
        public IKeyApi Key { get; private set; }

        /// <inheritdoc />
        public INameApi Name { get; private set; }

        /// <inheritdoc />
        public IObjectApi Object { get; private set; }

        /// <inheritdoc />
        public IPinApi Pin { get; private set; }

        /// <inheritdoc />
        public IPubSubApi PubSub { get; private set; }

        /// <inheritdoc />
        public ISwarmApi Swarm { get; private set; }

        internal async Task<Repository> Repository(CancellationToken cancel = default(CancellationToken))
        {
            Repository repo = new Repository
            {
                Options = Options.Repository
            };

            if (repositoryInited)
            {
                return await Task.FromResult(repo);
            }

            ;
            lock (this)
            {
                if (!repositoryInited)
                {
                    repositoryInited = true;
                }
            }
            await repo.CreateAsync(cancel);
            return repo;
        }

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
        public async Task<KeyChain> KeyChain(CancellationToken cancel = default(CancellationToken))
        {
            if (keyChain == null)
            {
                lock (this)
                {
                    if (keyChain == null)
                    {
                        keyChain = new KeyChain(this)
                        {
                            Options = Options.KeyChain
                        };
                     }
                }

                await keyChain.SetPassphraseAsync(passphrase, cancel);
                
                // Maybe create "self" key, this is the local peer's id.
                var self = await keyChain.FindKeyByNameAsync("self", cancel);
                if (self == null)
                {
                    self = await keyChain.CreateAsync("self", null, 0, cancel);
                }
            }
            return keyChain;
        }

        /// <summary>
        ///   Provides access to the local peer.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a <see cref="Peer"/>.
        /// </returns>
        public AsyncLazy<Peer> LocalPeer { get; private set; }

        /// <summary>
        ///   Starts the services.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   Starts the various IPFS and Peer2Peer services.  This should
        ///   be called after any configuration changes.
        /// </remarks>
        /// <exception cref="Exception">
        ///   When the engine is already started.
        /// </exception>
        public async Task StartAsync()
        {
            if (stopTasks.Count > 0)
            {
                throw new Exception("Already started");
            }

            var tasks = new List<Task>
            {
                new Task(async () =>
                {
                    var bootstrap = new Peer2Peer.Discovery.Bootstrap
                    {
                        Addresses = await this.Bootstrap.ListAsync()
                    };
                    bootstrap.PeerDiscovered += OnPeerDiscovered;
                    await bootstrap.StartAsync();
                    stopTasks.Add(new Task(async () => await bootstrap.StopAsync()));
                }),
                new Task(async () =>
                {
                    await SwarmService.StartAsync();
                    stopTasks.Add(new Task(async () => await SwarmService.StopAsync()));
                })
            };

            foreach(var task in tasks)
            {
                task.Start();
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        ///   Stops the running services.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   Multiple calls are okay.
        /// </remarks>
        public async Task StopAsync()
        {
            foreach (var task in stopTasks)
            {
                task.Start();
            }
            await Task.WhenAll(stopTasks);
            stopTasks = new List<Task>();
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>
        ///   Waits for <see cref="StopAsync"/> to complete.
        /// </remarks>
        public void Dispose()
        {
            StopAsync().Wait();
        }

        /// <summary>
        ///   Manages communication with other peers.
        /// </summary>
        public Swarm SwarmService { get; } = new Swarm();

        /// <summary>
        ///   Fired when a peer is discovered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        ///   Registers the peer with the <see cref="SwarmService"/>.
        /// </remarks>
        async void OnPeerDiscovered(object sender, PeerDiscoveredEventArgs e)
        {
            try
            {
                await SwarmService.RegisterPeerAsync(e.Address);
            }
            catch (Exception ex)
            {
                log.Warn("failed to register peer " + e.Address, ex);
                // eat it, nothing we can do.
            }
        }
    }
}
