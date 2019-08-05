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
using Ipfs.Engine.Migration;
using PeerTalk;
using System.Reflection;
using PeerTalk.Discovery;
using Nito.AsyncEx;
using Makaretu.Dns;
using System.Collections.Concurrent;
using System.Security;
using PeerTalk.SecureCommunication;
using PeerTalk.Cryptography;

namespace Ipfs.Engine
{
    /// <summary>
    ///   Implements the <see cref="Ipfs.CoreApi.ICoreApi">Core API</see> which makes it possible to create a decentralised and distributed 
    ///   application without relying on an "IPFS daemon".
    /// </summary>
    /// <remarks>
    ///   The engine should be used as a shared object in your program. It is thread safe (re-entrant) and conserves 
    ///   resources when only one instance is used.
    /// </remarks>
    public partial class IpfsEngine : ICoreApi, IService, IDisposable
    {
        static ILog log = LogManager.GetLogger(typeof(IpfsEngine));

        KeyChain keyChain;
        SecureString passphrase;
        ConcurrentBag<Func<Task>> stopTasks = new ConcurrentBag<Func<Task>>();

        /// <summary>
        ///   Creates a new instance of the <see cref="IpfsEngine"/> class
        ///   with the IPFS_PASS environment variable.
        /// </summary>
        /// <remarks>
        ///   Th passphrase must be in the IPFS_PASS environment variable.
        /// </remarks>
        public IpfsEngine()
        {
            var s = Environment.GetEnvironmentVariable("IPFS_PASS");
            if (s == null)
                throw new Exception("The IPFS_PASS environement variable is missing.");

            passphrase = new SecureString();
            foreach (var c in s)
            {
                this.passphrase.AppendChar(c);
            }
            Init();
        }

        /// <summary>
        ///   Creates a new instance of the <see cref="IpfsEngine"/> class
        ///   with the specified passphrase.
        /// </summary>
        /// <param name="passphrase">
        ///   The password used to access the keychain.
        /// </param>
        /// <remarks>
        ///   A <b>SecureString</b> copy of the passphrase is made so that the array can be 
        ///   zeroed out after the call.
        /// </remarks>
        public IpfsEngine(char[] passphrase)
        {
            this.passphrase = new SecureString();
            foreach (var c in passphrase)
            {
                this.passphrase.AppendChar(c);
            }
            Init();
        }

        /// <summary>
        ///   Creates a new instance of the <see cref="IpfsEngine"/> class
        ///   with the specified passphrase.
        /// </summary>
        /// <param name="passphrase">
        ///   The password used to access the keychain.
        /// </param>
        /// <remarks>
        ///  A copy of the <paramref name="passphrase"/> is made.
        /// </remarks>
        public IpfsEngine(SecureString passphrase)
        {
            this.passphrase = passphrase.Copy();
            Init();
        }

        void Init()
        { 
            // Init the core api inteface.
            Bitswap = new BitswapApi(this);
            Block = new BlockApi(this);
            BlockRepository = new BlockRepositoryApi(this);
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
            Stats = new StatsApi(this);
            Swarm = new SwarmApi(this);

            MigrationManager = new MigrationManager(this);

            // Async properties
            LocalPeer = new AsyncLazy<Peer>(async () =>
            {
                log.Debug("Building local peer");
                var keyChain = await KeyChainAsync().ConfigureAwait(false);
                log.Debug("Getting key info about self");
                var self = await keyChain.FindKeyByNameAsync("self").ConfigureAwait(false);
                var localPeer = new Peer
                {
                    Id = self.Id,
                    PublicKey = await keyChain.GetPublicKeyAsync("self").ConfigureAwait(false),
                    ProtocolVersion = "ipfs/0.1.0"
                };
                var version = typeof(IpfsEngine).GetTypeInfo().Assembly.GetName().Version;
                localPeer.AgentVersion = $"net-ipfs/{version.Major}.{version.Minor}.{version.Revision}";
                log.Debug("Built local peer");
                return localPeer;
            });
            SwarmService = new AsyncLazy<Swarm>(async () =>
            {
                log.Debug("Building swarm service");
                if (Options.Swarm.PrivateNetworkKey == null)
                {
                    var path = Path.Combine(Options.Repository.Folder, "swarm.key");
                    if (File.Exists(path))
                    {
                        using (var x = File.OpenText(path))
                        {
                            Options.Swarm.PrivateNetworkKey = new PreSharedKey();
                            Options.Swarm.PrivateNetworkKey.Import(x);
                        }
                    }
                }
                var peer = await LocalPeer.ConfigureAwait(false);
                var keyChain = await KeyChainAsync().ConfigureAwait(false);
                var self = await keyChain.GetPrivateKeyAsync("self").ConfigureAwait(false);
                var swarm = new Swarm
                {
                    LocalPeer = peer,
                    LocalPeerKey = PeerTalk.Cryptography.Key.CreatePrivateKey(self),
                    NetworkProtector = Options.Swarm.PrivateNetworkKey == null
                        ? null
                        : new Psk1Protector { Key = Options.Swarm.PrivateNetworkKey }
                };
                if (Options.Swarm.PrivateNetworkKey != null)
                    log.Debug($"Private network {Options.Swarm.PrivateNetworkKey.Fingerprint().ToHexString()}");

                log.Debug("Built swarm service");
                return swarm;
            });
            BitswapService = new AsyncLazy<BlockExchange.Bitswap>(async () =>
            {
                log.Debug("Building bitswap service");
                var bitswap = new BlockExchange.Bitswap
                {
                    Swarm = await SwarmService.ConfigureAwait(false),
                    BlockService = Block
                };
                log.Debug("Built bitswap service");
                return bitswap;
            });
            DhtService = new AsyncLazy<PeerTalk.Routing.Dht1>(async () =>
            {
                log.Debug("Building DHT service");
                var dht = new PeerTalk.Routing.Dht1
                {
                    Swarm = await SwarmService.ConfigureAwait(false)
                };
                dht.Swarm.Router = dht;
                log.Debug("Built DHT service");
                return dht;
            });
            PubSubService = new AsyncLazy<PeerTalk.PubSub.NotificationService>(async () =>
            {
                log.Debug("Building PubSub service");
                var pubsub = new PeerTalk.PubSub.NotificationService
                {
                    LocalPeer = await LocalPeer.ConfigureAwait(false)
                };
                pubsub.Routers.Add(new PeerTalk.PubSub.FloodRouter
                {
                    Swarm = await SwarmService.ConfigureAwait(false)
                });
                log.Debug("Built PubSub service");
                return pubsub;
            });
        }

        /// <summary>
        ///   The configuration options.
        /// </summary>
        public IpfsEngineOptions Options { get; set; } = new IpfsEngineOptions();

        /// <summary>
        ///   Manages the version of the repository.
        /// </summary>
        public MigrationManager MigrationManager { get; set; }

        /// <inheritdoc />
        public IBitswapApi Bitswap { get; set; }

        /// <inheritdoc />
        public IBlockApi Block { get; set; }

        /// <inheritdoc />
        public IBlockRepositoryApi BlockRepository { get; set; }

        /// <inheritdoc />
        public IBootstrapApi Bootstrap { get; set; }

        /// <inheritdoc />
        public IConfigApi Config { get; set; }

        /// <inheritdoc />
        public IDagApi Dag { get; set; }

        /// <inheritdoc />
        public IDhtApi Dht { get; set; }

        /// <inheritdoc />
        public IDnsApi Dns { get; set; }

        /// <inheritdoc />
        public IFileSystemApi FileSystem { get; set; }

        /// <inheritdoc />
        public IGenericApi Generic { get; set; }

        /// <inheritdoc />
        public IKeyApi Key { get; set; }

        /// <inheritdoc />
        public INameApi Name { get; set; }

        /// <inheritdoc />
        public IObjectApi Object { get; set; }

        /// <inheritdoc />
        public IPinApi Pin { get; set; }

        /// <inheritdoc />
        public IPubSubApi PubSub { get; set; }

        /// <inheritdoc />
        public ISwarmApi Swarm { get; set; }

        /// <inheritdoc />
        public IStatsApi Stats { get; set; }

        /// <summary>
        ///   Provides access to the <see cref="KeyChain"/>.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the <see cref="KeyChain"/>.
        /// </returns>
        public async Task<KeyChain> KeyChainAsync(CancellationToken cancel = default(CancellationToken))
        {
            // TODO: this should be a LazyAsync property.
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

                await keyChain.SetPassphraseAsync(passphrase, cancel).ConfigureAwait(false);
                
                // Maybe create "self" key, this is the local peer's id.
                var self = await keyChain.FindKeyByNameAsync("self", cancel).ConfigureAwait(false);
                if (self == null)
                {
                    self = await keyChain.CreateAsync("self", null, 0, cancel).ConfigureAwait(false);
                }
            }
            return keyChain;
        }

        /// <summary>
        ///   Provides access to the local peer.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a <see cref="Ipfs.Peer"/>.
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
            var r = await Generic.ResolveAsync(path, true, cancel).ConfigureAwait(false);
            return Cid.Decode(r.Remove(0, 6));  // strip '/ipfs/'.
        }

        /// <summary>
        ///   Determines if the engine has started.
        /// </summary>
        /// <value>
        ///   <b>true</b> if the engine has started; otherwise, <b>false</b>.
        /// </value>
        /// <seealso cref="Start"/>
        /// <seealso cref="StartAsync"/>
        public bool IsStarted => stopTasks.Count > 0;

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

            // Repository must be at the correct version.
            await MigrationManager.MirgrateToVersionAsync(MigrationManager.LatestVersion)
                .ConfigureAwait(false);

            var localPeer = await LocalPeer.ConfigureAwait(false);
            log.Debug("starting " + localPeer.Id);

            // Everybody needs the swarm.
            var swarm = await SwarmService.ConfigureAwait(false);
            stopTasks.Add(async () =>
            {
                await swarm.StopAsync().ConfigureAwait(false);
            });
            await swarm.StartAsync().ConfigureAwait(false);

            // Start the primary services.
            var tasks = new List<Func<Task>>
            {
                async () =>
                {
                    var bitswap = await BitswapService.ConfigureAwait(false);
                    stopTasks.Add(async () => await bitswap.StopAsync().ConfigureAwait(false));
                    await bitswap.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    var dht = await DhtService.ConfigureAwait(false);
                    stopTasks.Add(async () => await dht.StopAsync().ConfigureAwait(false));
                    await dht.StartAsync().ConfigureAwait(false);
                },
                async () =>
                {
                    var pubsub = await PubSubService.ConfigureAwait(false);
                    stopTasks.Add(async () => await pubsub.StopAsync().ConfigureAwait(false));
                    await pubsub.StartAsync().ConfigureAwait(false);
                },
            };

            log.Debug("waiting for services to start");
            await Task.WhenAll(tasks.Select(t => t())).ConfigureAwait(false);

            // Starting listening to the swarm.
            var json = await Config.GetAsync("Addresses.Swarm").ConfigureAwait(false);
            var numberListeners = 0;
            foreach (string a in json)
            {
                try
                {
                    await swarm.StartListeningAsync(a).ConfigureAwait(false);
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

            // Now that the listener addresses are established, the discovery 
            // services can begin.
            MulticastService multicast = null;
            if (!Options.Discovery.DisableMdns)
            {
                multicast = new MulticastService();
#pragma warning disable CS1998
                stopTasks.Add(async () => multicast.Dispose());
#pragma warning restore CS1998
            }

            var autodialer = new AutoDialer(swarm)
            {
                MinConnections = Options.Swarm.MinConnections
            };
#pragma warning disable CS1998
            stopTasks.Add(async () => autodialer.Dispose());
#pragma warning restore CS1998

            tasks = new List<Func<Task>>
            {
                // Bootstrap discovery
                async () =>
                {
                    var bootstrap = new PeerTalk.Discovery.Bootstrap
                    {
                        Addresses = await this.Bootstrap.ListAsync()
                    };
                    bootstrap.PeerDiscovered += OnPeerDiscovered;
                    stopTasks.Add(async () => await bootstrap.StopAsync().ConfigureAwait(false));
                    await bootstrap.StartAsync().ConfigureAwait(false);
                },
                // New multicast DNS discovery
                async () =>
                {
                    if (Options.Discovery.DisableMdns)
                        return;
                    var mdns = new PeerTalk.Discovery.MdnsNext
                    {
                        LocalPeer = localPeer,
                        MulticastService = multicast
                    };
                    if (Options.Swarm.PrivateNetworkKey != null)
                    {
                        mdns.ServiceName = $"_p2p-{Options.Swarm.PrivateNetworkKey.Fingerprint().ToHexString()}._udp";
                    }
                    mdns.PeerDiscovered += OnPeerDiscovered;
                    stopTasks.Add(async () => await mdns.StopAsync().ConfigureAwait(false));
                    await mdns.StartAsync().ConfigureAwait(false);
                },
                // Old style JS multicast DNS discovery
                async () =>
                {
                    if (Options.Discovery.DisableMdns || Options.Swarm.PrivateNetworkKey != null)
                        return;
                    var mdns = new PeerTalk.Discovery.MdnsJs
                    {
                        LocalPeer = localPeer,
                        MulticastService = multicast
                    };
                    mdns.PeerDiscovered += OnPeerDiscovered;
                    stopTasks.Add(async () => await mdns.StopAsync().ConfigureAwait(false));
                    await mdns.StartAsync().ConfigureAwait(false);
                },
                // Old style GO multicast DNS discovery
                async () =>
                {
                    if (Options.Discovery.DisableMdns || Options.Swarm.PrivateNetworkKey != null)
                        return;
                    var mdns = new PeerTalk.Discovery.MdnsGo
                    {
                        LocalPeer = localPeer,
                        MulticastService = multicast
                    };
                    mdns.PeerDiscovered += OnPeerDiscovered;
                    stopTasks.Add(async () => await mdns.StopAsync().ConfigureAwait(false));
                    await mdns.StartAsync().ConfigureAwait(false);
                },
                async () => 
                {
                    if (Options.Discovery.DisableRandomWalk)
                        return;
                    var randomWalk = new RandomWalk { Dht = Dht };
                    stopTasks.Add(async () => await randomWalk.StopAsync().ConfigureAwait(false));
                    await randomWalk.StartAsync().ConfigureAwait(false);
                }
            };
            log.Debug("waiting for discovery services to start");
            await Task.WhenAll(tasks.Select(t => t())).ConfigureAwait(false);

            multicast?.Start();

            log.Debug("started");
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
                stopTasks = new ConcurrentBag<Func<Task>>();
                await Task.WhenAll(tasks.Select(t => t())).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Error("Failure when stopping the engine", e);
            }

            // Many services use cancellation to stop.  A cancellation may not run
            // immediately, so we need to give them some.
            // TODO: Would be nice to make this deterministic.
            await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);

            log.Debug("stopped");
        }

        /// <summary>
        ///   A synchronous start.
        /// </summary>
        /// <remarks>
        ///   Calls <see cref="StartAsync"/> and waits for it to complete.
        /// </remarks>
        public void Start()
        {
            StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        ///   A synchronous stop.
        /// </summary>
        /// <remarks>
        ///   Calls <see cref="StopAsync"/> and waits for it to complete.
        /// </remarks>
        public void Stop()
        {
            log.Debug("stopping");
            try
            {
                var tasks = stopTasks.ToArray();
                stopTasks = new ConcurrentBag<Func<Task>>();
                foreach (var task in tasks)
                {
                    task().ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
            catch (Exception e)
            {
                log.Error("Failure when stopping the engine", e);
            }
        }

        /// <summary>
        ///   Manages communication with other peers.
        /// </summary>
        public AsyncLazy<Swarm> SwarmService { get; private set; }

        /// <summary>
        ///   Manages publishng and subscribing to messages.
        /// </summary>
        public AsyncLazy<PeerTalk.PubSub.NotificationService> PubSubService { get; private set; }

        /// <summary>
        ///   Exchange blocks with other peers.
        /// </summary>
        public AsyncLazy<BlockExchange.Bitswap> BitswapService { get; private set; }

        /// <summary>
        ///   Finds information with a distributed hash table.
        /// </summary>
        public AsyncLazy<PeerTalk.Routing.Dht1> DhtService { get; private set; }

#pragma warning disable VSTHRD100 // Avoid async void methods
        /// <summary>
        ///   Fired when a peer is discovered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="peer"></param>
        /// <remarks>
        ///   Registers the peer with the <see cref="SwarmService"/>.
        /// </remarks>
        async void OnPeerDiscovered(object sender, Peer peer)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                var swarm = await SwarmService.ConfigureAwait(false);
                swarm.RegisterPeer(peer);
            }
            catch (Exception ex)
            {
                log.Warn("failed to register peer " + peer, ex);
                // eat it, nothing we can do.
            }
        }

        #region IDisposable Support
        bool disposedValue = false; // To detect redundant calls

        /// <summary>
        ///  Releases the unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <b>true</b> to release both managed and unmanaged resources; <b>false</b> 
        ///   to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;

                if (disposing)
                {
                    passphrase?.Dispose();
                    Stop();
                }
            }
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
