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
using Makaretu.Dns;
using System.Collections.Concurrent;

namespace Ipfs.Engine
{
    /// <summary>
    ///    Implements the <see cref="ICoreApi">Core API</see> which makes it possible to create a decentralised and distributed 
    ///    application without relying on an "IPFS daemon".
    /// </summary>
    /// <remarks>
    ///   The engine should be used as a shared object in your program. It is thread safe (re-entrant) and conserves 
    ///   resources when only one instance is used.
    /// </remarks>
    public partial class IpfsEngine : ICoreApi, IService, IDisposable
    {
        static ILog log = LogManager.GetLogger(typeof(IpfsEngine));

        KeyChain keyChain;
        char[] passphrase;
        ConcurrentBag<Func<Task>> stopTasks = new ConcurrentBag<Func<Task>>();

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
            Stats = new StatsApi(this);
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
                var peer = await LocalPeer;
                var keyChain = await KeyChain();
                var self = await keyChain.GetPrivateKeyAsync("self");
                var swarm = new Swarm
                {
                    LocalPeer = peer,
                    LocalPeerKey = PeerTalk.Cryptography.Key.CreatePrivateKey(self)
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
            DhtService = new AsyncLazy<PeerTalk.Routing.Dht1>(async () =>
            {
                log.Debug("Building DHT service");
                var dht = new PeerTalk.Routing.Dht1
                {
                    Swarm = await SwarmService
                };
                dht.Swarm.Router = dht;
                log.Debug("Built DHT service");
                return dht;
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

        /// <inheritdoc />
        public IStatsApi Stats { get; private set; }

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
            var swarm = await SwarmService;
            stopTasks.Add(async () => await swarm.StopAsync());
            await swarm.StartAsync();

            var multicast = new MulticastService();
#pragma warning disable CS1998 
            stopTasks.Add(async () => multicast.Dispose());
#pragma warning restore CS1998 

            var tasks = new List<Func<Task>>
            {
                async () =>
                {
                    var bootstrap = new PeerTalk.Discovery.Bootstrap
                    {
                        Addresses = await this.Bootstrap.ListAsync()
                    };
                    bootstrap.PeerDiscovered += OnPeerDiscovered;
                    stopTasks.Add(async () => await bootstrap.StopAsync());
                    await bootstrap.StartAsync();
                },
                async () =>
                {
                    var mdns = new PeerTalk.Discovery.MdnsNext
                    {
                        LocalPeer = localPeer,
                        MulticastService = multicast
                    };
                    mdns.PeerDiscovered += OnPeerDiscovered;
                    stopTasks.Add(async () => await mdns.StopAsync());
                    await mdns.StartAsync();
                },
                async () =>
                {
                    var mdns = new PeerTalk.Discovery.MdnsJs
                    {
                        LocalPeer = localPeer,
                        MulticastService = multicast
                    };
                    mdns.PeerDiscovered += OnPeerDiscovered;
                    stopTasks.Add(async () => await mdns.StopAsync());
                    await mdns.StartAsync();
                },
                async () =>
                {
                    var mdns = new PeerTalk.Discovery.MdnsGo
                    {
                        LocalPeer = localPeer,
                        MulticastService = multicast
                    };
                    mdns.PeerDiscovered += OnPeerDiscovered;
                    stopTasks.Add(async () => await mdns.StopAsync());
                    await mdns.StartAsync();
                },
                async () =>
                {
                    var bitswap = await BitswapService;
                    stopTasks.Add(async () => await bitswap.StopAsync());
                    await bitswap.StartAsync();
                },
                async () =>
                {
                    var dht = await DhtService;
                    stopTasks.Add(async () => await dht.StopAsync());
                    await dht.StartAsync();
                },
            };

            log.Debug("waiting for services to start");
            await Task.WhenAll(tasks.Select(t => t()));

            // TODO: Would be nice to make this deterministic.
            //await Task.Delay(TimeSpan.FromMilliseconds(100));
            //log.Debug("all service started");

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

            // Now that the listener addresses are established, the mdns discovery can begin.
            // TODO: Maybe all discovery services should be start here.
            multicast.Start();

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
                await Task.WhenAll(tasks.Select(t => t()));
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
        ///   Finds information with a distributed hash table.
        /// </summary>
        public AsyncLazy<PeerTalk.Routing.Dht1> DhtService { get; private set; }

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
