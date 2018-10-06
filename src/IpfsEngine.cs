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
using PeerTalk;
using System.Reflection;
using PeerTalk.Discovery;
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
            Dns = new DnsApi(this);
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
                log.Debug("Getting key info about self");
                var self = await keyChain.FindKeyByNameAsync("self");
                var localPeer = new Peer();
                localPeer.Id = self.Id;
                localPeer.PublicKey = await keyChain.GetPublicKeyAsync("self");
                localPeer.ProtocolVersion = "ipfs/0.1.0";
                var version = typeof(IpfsEngine).GetTypeInfo().Assembly.GetName().Version;
                localPeer.AgentVersion = $"net-ipfs/{version.Major}.{version.Minor}.{version.Revision}";
                log.Debug("Built local peer");
                return localPeer;
            });
            SwarmService = new AsyncLazy<Swarm>(async () =>
            {
                log.Debug("Building swarm service");
                var swarm = new Swarm
                {
                    LocalPeer = await LocalPeer
                };
                log.Debug("Built swarm service");
                return swarm;
            });
            BitswapService = new AsyncLazy<BlockExchange.Bitswap>(async () =>
            {
                log.Debug("Building bitswap service");
                var bitswap = new BlockExchange.Bitswap
                {
                    Swarm = await SwarmService,
                    BlockService = Block
                };
                log.Debug("Built bitswap service");
                return bitswap;
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
        public IDnsApi Dns { get; private set; }

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

        internal Task<Repository> Repository(CancellationToken cancel = default(CancellationToken))
        {
            Repository repo = new Repository
            {
                Options = Options.Repository
            };

            if (!repositoryInited)
            {
                lock (this)
                {
                    if (!repositoryInited)
                    {
                        repo.CreateAsync(cancel).Wait();
                        repositoryInited = true;
                    }
                }
            }
            return Task.FromResult(repo);
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
        public async Task<Cid> ResolveIpfsPathToCidAsync (string path, CancellationToken cancel = default(CancellationToken))
        {
            var r = await Generic.ResolveAsync(path, true, cancel);
            return Cid.Decode(r.Remove(0, 6));  // strip '/ipfs/'.
        }

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
        public async Task StartAsync()
        {
            if (stopTasks.Count > 0)
            {
                throw new Exception("IPFS engine is already started.");
            }

            var localPeer = await LocalPeer;
            log.Debug("starting " + localPeer.Id);

            // Everybody needs the swarm.
            var swarm = await StartSwarmAsync();

            var tasks = new List<Task>
            {
                new Task(async () =>
                {
                    var bootstrap = new PeerTalk.Discovery.Bootstrap
                    {
                        Addresses = await this.Bootstrap.ListAsync()
                    };
                    bootstrap.PeerDiscovered += OnPeerDiscovered;
                    stopTasks.Add(new Task(async () => await bootstrap.StopAsync()));
                    await bootstrap.StartAsync();
                }),
                new Task(async () =>
                {
                    var mdns = new PeerTalk.Discovery.Mdns
                    {
                        LocalPeer = localPeer
                    };
                    mdns.PeerDiscovered += OnPeerDiscovered;
                    swarm.ListenerEstablished += (s, e) =>
                    {
                        mdns.RefreshPeer();
                    };
                    stopTasks.Add(new Task(async () => await mdns.StopAsync()));
                    await mdns.StartAsync();
                }),
                new Task(async () =>
                {
                    var bitswap = await BitswapService;
                    stopTasks.Add(new Task(async () => await bitswap.StopAsync()));
                    await bitswap.StartAsync();
                }),
            };

            foreach(var task in tasks)
            {
                task.Start();
            }

            log.Debug("waiting for services to start");
            await Task.WhenAll(tasks);
            // TODO: Would be nice to make this deterministic.
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            log.Debug("all service started");

            // Starting listening to the swarm.
            var json = await Config.GetAsync("Addresses.Swarm");
            var numberListeners = 0;
            foreach (string a in json)
            {
                try
                {
                    await swarm.StartListeningAsync(a);
                    ++numberListeners;
                }
                catch (Exception e)
                {
                    log.Warn($"Listener failure for '{a}'", e);
                    // eat the exception
                }
            }
            if (numberListeners == 0)
            {
                log.Error("No listeners were created.");
            }

            log.Debug("started");
        }

        async Task<Swarm> StartSwarmAsync()
        {
            var swarm = await SwarmService;
            stopTasks.Add(new Task(async () => await swarm.StopAsync()));
            await swarm.StartAsync();

            return swarm;
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
            log.Debug("stopping");
            try
            {
                var tasks = stopTasks.ToArray();
                stopTasks = new List<Task>();
                foreach (var task in tasks)
                {
                    task.Start();
                }
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                log.Error("Failure when stopping the engine", e);
            }

            // Many services use cancellation to stop.  A cancellation may not run
            // immediately, so we need to give them some.
            // TODO: Would be nice to make this deterministic.
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            log.Debug("stopped");
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
        public AsyncLazy<Swarm> SwarmService { get; private set; }

        /// <summary>
        ///   Exchange blocks with other peers.
        /// </summary>
        public AsyncLazy<BlockExchange.Bitswap> BitswapService { get; private set; }

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
                var swarm = await SwarmService;
                var peer = await swarm.RegisterPeerAsync(e.Address);
            }
            catch (Exception ex)
            {
                log.Warn("failed to register peer " + e.Address, ex);
                // eat it, nothing we can do.
            }
        }
    }
}
