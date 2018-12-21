using Ipfs;
using PeerTalk.Transports;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using Common.Logging;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using PeerTalk.Protocols;
using PeerTalk.Cryptography;
using PeerTalk.Discovery;
using PeerTalk.Routing;

namespace PeerTalk
{
    /// <summary>
    ///   Manages communication with other peers.
    /// </summary>
    public class Swarm : IService, IPolicy<MultiAddress>
    {
        static ILog log = LogManager.GetLogger(typeof(Swarm));

        /// <summary>
        ///  The supported protocols.
        /// </summary>
        /// <remarks>
        ///   Use sychronized access, e.g. <code>lock (protocols) { ... }</code>.
        /// </remarks>
        List<IPeerProtocol> protocols = new List<IPeerProtocol>
        {
            new Multistream1(),
            new SecureCommunication.Secio1(),
            new Identify1(),
            new Mplex67()
        };

        /// <summary>
        ///   Added to connection protocols when needed.
        /// </summary>
        Plaintext1 plaintext1 = new Plaintext1();

        Peer localPeer;

        /// <summary>
        ///   Raised when a listener is establihed.
        /// </summary>
        /// <remarks>
        ///   Raised when <see cref="StartListeningAsync(MultiAddress)"/>
        ///   succeeds.
        /// </remarks>
        public event EventHandler<Peer> ListenerEstablished;

        /// <summary>
        ///   Raised when a connection to another peer is established.
        /// </summary>
        public event EventHandler<PeerConnection> ConnectionEstablished;

        /// <summary>
        ///   Raised when a new peer is discovered for the first time.
        /// </summary>
        public event EventHandler<Peer> PeerDiscovered;

        /// <summary>
        ///  The local peer.
        /// </summary>
        /// <value>
        ///   The local peer must have an <see cref="Peer.Id"/> and
        ///   <see cref="Peer.PublicKey"/>.
        /// </value>
        public Peer LocalPeer
        {
            get { return localPeer; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (value.Id == null)
                    throw new ArgumentNullException("peer.Id");
                if (value.PublicKey == null)
                    throw new ArgumentNullException("peer.PublicKey");
                if (!value.IsValid())
                    throw new ArgumentException("Invalid peer.");
                localPeer = value;
            }
        }

        /// <summary>
        ///   The private key of the local peer.
        /// </summary>
        /// <value>
        ///   Used to prove the identity of the <see cref="LocalPeer"/>.
        /// </value>
        public Key LocalPeerKey { get; set; }

        /// <summary>
        ///   Other nodes. Key is the bae58 hash of the peer ID.
        /// </summary>
        ConcurrentDictionary<string, Peer> otherPeers = new ConcurrentDictionary<string, Peer>();

        /// <summary>
        ///   Manages the swarm's peer connections.
        /// </summary>
        public ConnectionManager Manager = new ConnectionManager();

        /// <summary>
        ///   Use to find addresses of a peer.
        /// </summary>
        public IPeerRouting Router { get; set; }

        /// <summary>
        ///   Cancellation tokens for the listeners.
        /// </summary>
        ConcurrentDictionary<MultiAddress, CancellationTokenSource> listeners = new ConcurrentDictionary<MultiAddress, CancellationTokenSource>();

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
            get
            {
                return otherPeers
                    .Values
                    .SelectMany(p => p.Addresses);
            }
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
                return otherPeers.Values;
            }
        }

        /// <summary>
        ///   Register that a peer's address has been discovered.
        /// </summary>
        /// <param name="address">
        ///   An address to the peer. It must end with the peer ID.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the <see cref="Peer"/> that is registered.
        /// </returns>
        /// <exception cref="Exception">
        ///   The <see cref="BlackList"/> or <see cref="WhiteList"/> policies forbid it.
        ///   Or the "p2p/ipfs" protocol name is missing.
        /// </exception>
        /// <remarks>
        ///   If the <paramref name="address"/> is not already known, then it is
        ///   added to the <see cref="KnownPeerAddresses"/>.
        /// </remarks>
        public async Task<Peer> RegisterPeerAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            var peerId = address.PeerId;
            if (peerId == LocalPeer.Id)
            {
                throw new Exception("Cannot register to self.");
            }

            if (!await IsAllowedAsync(address, cancel))
            {
                throw new Exception($"Communication with '{address}' is not allowed.");
            }

            var isNew = false;
            var p = otherPeers.AddOrUpdate(peerId.ToBase58(),
                (id) => {
                    log.Debug("new peer " + peerId);
                    isNew = true;
                    return new Peer
                    {
                        Id = id,
                        Addresses = new List<MultiAddress> { address }
                    };
                },
                (id, peer) =>
                {
                    peer.Addresses = peer.Addresses.ToList();
                    var addrs = (List<MultiAddress>)peer.Addresses;
                    if (!addrs.Contains(address))
                    {
                        addrs.Add(address);
                    }
                    return peer;
                });

            if (isNew)
            {
                PeerDiscovered?.Invoke(this, p);
            }

            return p;
        }

        /// <summary>
        ///   Register that a peer has been discovered.
        /// </summary>
        /// <param name="peer">
        ///   The newly discovered peer.
        /// </param>
        /// <returns>
        ///   The registered peer.
        /// </returns>
        /// <remarks>
        ///   If the peer already exists, then the existing peer is updated with supplied
        ///   information and is then returned.  Otherwise, the <paramref name="peer"/>
        ///   is added to known peers and is returned.
        ///   <para>
        ///   If the peer already exists, then a union of the existing and new addresses
        ///   is used.  For all other information the <paramref name="peer"/>'s information
        ///   is used if not <b>null</b>.
        ///   </para>
        ///   <para>
        ///   If peer does not already exist, then the <see cref="PeerDiscovered"/> event
        ///   is raised.
        ///   </para>
        /// </remarks>
        public Peer RegisterPeer(Peer peer)
        {
            if (peer.Id == null)
            {
                throw new ArgumentNullException("peer.ID");
            }
            if (peer.Id == LocalPeer.Id)
            {
                throw new ArgumentException("Cannot register self.");
            }

            var isNew = false;
            var p = otherPeers.AddOrUpdate(peer.Id.ToBase58(),
                (id) =>
                {
                    isNew = true;
                    return peer;
                },
                (id, existing) =>
                {
                    if (!Object.ReferenceEquals(existing, peer))
                    {
                        existing.AgentVersion = peer.AgentVersion ?? existing.AgentVersion;
                        existing.ProtocolVersion = peer.ProtocolVersion ?? existing.ProtocolVersion;
                        existing.PublicKey = peer.PublicKey ?? existing.PublicKey;
                        existing.Latency = peer.Latency ?? existing.Latency;
                        existing.Addresses = existing
                            .Addresses
                            .Union(peer.Addresses)
                            .ToList();
                    }
                    return existing;
                });

            if (isNew)
            {
                PeerDiscovered?.Invoke(this, p);
            }

            return p;
        }

        /// <summary>
        ///   The addresses that cannot be used.
        /// </summary>
        public BlackList<MultiAddress> BlackList { get; set; } = new BlackList<MultiAddress>();

        /// <summary>
        ///   The addresses that can be used.
        /// </summary>
        public WhiteList<MultiAddress> WhiteList { get; set; } = new WhiteList<MultiAddress>();

        /// <inheritdoc />
        public Task StartAsync()
        {
            if (LocalPeer == null)
            {
                throw new NotSupportedException("The LocalPeer is not defined.");
            }

            // Many of the unit tests do not setup the LocalPeerKey.  If
            // its missing, then just use plaintext connection.
            // TODO: make the tests setup the security protocols.
            if (LocalPeerKey == null)
            {
                lock (protocols)
                {
                    var security = protocols.OfType<IEncryptionProtocol>().ToArray();
                    foreach (var p in security)
                    {
                        protocols.Remove(p);
                    }
                    protocols.Add(plaintext1);
                }
                log.Warn("Peer key is missing, using unencrypted connections.");
            }

            log.Debug("Starting");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task StopAsync()
        {
            log.Debug($"Stoping {LocalPeer}");

            // Stop the listeners.
            while (listeners.Count > 0)
            {
                await StopListeningAsync(listeners.Keys.First());
            }

            // Disconnect from remote peers.
            Manager.Clear();

            otherPeers.Clear();
            listeners.Clear();
            BlackList = new BlackList<MultiAddress>();
            WhiteList = new WhiteList<MultiAddress>();

            log.Debug($"Stoped {LocalPeer}");
        }


        /// <summary>
        ///   Connect to a peer.
        /// </summary>
        /// <param name="address">
        ///   An ipfs <see cref="MultiAddress"/>, such as
        ///  <c>/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ</c>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the connected <see cref="Peer"/>.
        /// </returns>
        /// <remarks>
        ///   If already connected to the peer, on any address, then nothing is done.
        /// </remarks>
        public async Task<Peer> ConnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            var peer = await RegisterPeerAsync(address, cancel);
            return await ConnectAsync(peer, cancel);
        }

        /// <summary>
        ///   Connect to a peer.
        /// </summary>
        /// <param name="peer">
        ///  A peer to connect to.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the connected <see cref="Peer"/>.
        /// </returns>
        /// <remarks>
        ///   If already connected to the peer, then nothing is done.
        /// </remarks>
        public async Task<Peer> ConnectAsync(Peer peer, CancellationToken cancel = default(CancellationToken))
        {
            peer = RegisterPeer(peer);

            // If connected and still open, then use the existing connection.
            if (Manager.TryGet(peer, out PeerConnection _))
            {
                return peer;
            }

            // Establish a stream.
            var connection = await Dial(peer, peer.Addresses, cancel);

            return peer;
        }

        /// <summary>
        ///   Create a stream to the peer that talks the specified protocol.
        /// </summary>
        /// <param name="peer">
        ///   The remote peer.
        /// </param>
        /// <param name="protocol">
        ///   The protocol name, such as "/foo/0.42.0".
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the new <see cref="Stream"/> to the <paramref name="peer"/>.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///   When finished, the caller must <see cref="Stream.Dispose()"/> the
        ///   new stream.
        ///   </para>
        /// </remarks>
        public async Task<Stream> DialAsync(Peer peer, string protocol, CancellationToken cancel = default(CancellationToken))
        {
            peer = RegisterPeer(peer);

            // Get a connection and then a muxer to the peer.
            var _ = await ConnectAsync(peer, cancel);
            if (!Manager.TryGet(peer, out PeerConnection connection))
            {
                throw new Exception($"Cannot establish connection to peer '{peer}'.");
            }
            var muxer = await connection.MuxerEstablished.Task;

            // Create a new stream for the peer protocol.
            var stream = await muxer.CreateStreamAsync(protocol);
            try
            {
                await connection.EstablishProtocolAsync("/multistream/", stream);

                await Message.WriteAsync(protocol, stream, cancel);
                var result = await Message.ReadStringAsync(stream, cancel);
                if (result != protocol)
                {
                    throw new Exception($"Protocol '{protocol}' not supported by '{peer}'.");
                }

                return stream;
            }
            catch (Exception)
            {
                stream.Dispose();
                throw;
            }
        }

        /// <summary>
        ///   Establish a duplex stream between the local and remote peer.
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="addrs"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        async Task<PeerConnection> Dial(Peer remote, IEnumerable<MultiAddress> addrs, CancellationToken cancel)
        {
            if (remote == LocalPeer)
            {
                throw new Exception("Cannot dial self.");
            }

            // If no addresses, then ask peer routing.
            if (Router != null && addrs.Count() == 0)
            {
                var found = await Router.FindPeerAsync(remote.Id, cancel);
                addrs = found.Addresses;
                remote.Addresses = addrs;
            }

            // Get the addresses we can use to dial the remote.
            var possibleAddresses = (await Task.WhenAll(addrs.Select(a => a.ResolveAsync(cancel))))
                .SelectMany(a => a)
                .Select(a => a.WithPeerId(remote.Id))
                .Distinct()
                .ToArray();
            // TODO: filter out self addresses and others.
            if (possibleAddresses.Length == 0)
            {
                throw new Exception($"{remote} has no known addresses.");
            }

            // Try the various addresses in parallel.  The first one to complete wins.
            // Timeout on connection is 3 seconds.  TODO: not very interplanetary!
            PeerConnection connection = null;
            try
            {
                using (var timeout = new CancellationTokenSource(3000))
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancel))
                {
                    var attempts = possibleAddresses
                        .Select(a => DialAsync(remote, a, cts.Token));
                    connection = await TaskHelper.WhenAnyResult(attempts, cts.Token);
                    cts.Cancel(); // stop other dialing tasks.
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Cannot dial {remote}.", e);
            }

            // Do the connection handshake.
            try
            {
                MountProtocols(connection);
                IEncryptionProtocol[] security = null;
                lock (protocols)
                {
                    security = protocols.OfType<IEncryptionProtocol>().ToArray();
                }
                await connection.InitiateAsync(security, cancel);
                await connection.MuxerEstablished.Task;
                Identify1 identify = null;
                lock (protocols)
                {
                    identify = protocols.OfType<Identify1>().First();
                }
                await identify.GetRemotePeer(connection, cancel);
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }

            var actual = Manager.Add(connection);
            if (actual == connection)
            {
                ConnectionEstablished?.Invoke(this, connection);
            }

            return actual;

        }

        async Task<PeerConnection> DialAsync(Peer remote, MultiAddress addr, CancellationToken cancel)
        {
            // TODO: HACK: Currenty only the ipfs/p2p is supported.
            // short circuit to make life faster.
            if (addr.Protocols.Count != 3
                || !(addr.Protocols[2].Name == "ipfs" || addr.Protocols[2].Name == "p2p"))
            {
                throw new Exception($"Cannnot dial; unknown protocol in '{addr}'.");
            }

            // Establish the transport stream.
            Stream stream = null;
            foreach (var protocol in addr.Protocols)
            {
                cancel.ThrowIfCancellationRequested();
                if (TransportRegistry.Transports.TryGetValue(protocol.Name, out Func<IPeerTransport> transport))
                {
                    stream = await transport().ConnectAsync(addr, cancel);
                    if (cancel.IsCancellationRequested)
                    {
                        stream?.Dispose();
                        continue;
                    }
                    break;
                }
            }
            if (stream == null)
            {
                throw new Exception("Missing a known transport protocol name.");
            }

            // Build the connection.
            remote.ConnectedAddress = addr;
            var connection = new PeerConnection
            {
                IsIncoming = false,
                LocalPeer = LocalPeer,
                // TODO: LocalAddress
                LocalPeerKey = LocalPeerKey,
                RemotePeer = remote,
                RemoteAddress = addr,
                Stream = stream
            };

            return connection;
        }

        /// <summary>
        ///   Disconnect from a peer.
        /// </summary>
        /// <param name="address">
        ///   An ipfs <see cref="MultiAddress"/>, such as
        ///  <c>/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ</c>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   If the peer is not conected, then nothing happens.
        /// </remarks>
        public Task DisconnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            Manager.Remove(address.PeerId);
            return Task.CompletedTask;
        }

        /// <summary>
        ///   Start listening on the specified <see cref="MultiAddress"/>.
        /// </summary>
        /// <param name="address">
        ///   Typically "/ip4/0.0.0.0/tcp/4001" or "/ip6/::/tcp/4001".
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.  The task's result
        ///   is a <see cref="MultiAddress"/> than can be used by another peer
        ///   to connect to tis peer.
        /// </returns>
        /// <exception cref="Exception">
        ///   Already listening on <paramref name="address"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="address"/> is missing a transport protocol (such as tcp or udp).
        /// </exception>
        /// <remarks>
        ///   Allows other peers to <see cref="ConnectAsync(MultiAddress, CancellationToken)">connect</see>
        ///   to the <paramref name="address"/>.
        ///   <para>
        ///   The <see cref="Peer.Addresses"/> of the <see cref="LocalPeer"/> are updated.  If the <paramref name="address"/> refers to
        ///   any IP address ("/ip4/0.0.0.0" or "/ip6/::") then all network interfaces addresses
        ///   are added.  If the port is zero (as in "/ip6/::/tcp/0"), then the peer addresses contains the actual port number
        ///   that was assigned.
        ///   </para>
        /// </remarks>
        public Task<MultiAddress> StartListeningAsync(MultiAddress address)
        {
            var cancel = new CancellationTokenSource();

            if (!listeners.TryAdd(address, cancel))
            {
                throw new Exception($"Already listening on '{address}'.");
            }

            // Start a listener for the transport
            var didSomething = false;
            foreach (var protocol in address.Protocols)
            {
                if (TransportRegistry.Transports.TryGetValue(protocol.Name, out Func<IPeerTransport> transport))
                {
                    address = transport().Listen(address, OnRemoteConnect, cancel.Token);
                    listeners.TryAdd(address, cancel);
                    didSomething = true;
                    break;
                }
            }
            if (!didSomething)
            {
                throw new ArgumentException($"Missing a transport protocol name '{address}'.", "address");
            }

            var result = new MultiAddress($"{address}/ipfs/{LocalPeer.Id}");

            // Get the actual IP address(es).
            IEnumerable<MultiAddress> addresses = new List<MultiAddress>();
            var ips = NetworkInterface.GetAllNetworkInterfaces()
                // It appears that the loopback adapter is not UP on Ubuntu 14.04.5 LTS
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                    || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                .SelectMany(nic => nic.GetIPProperties().UnicastAddresses);
            if (result.ToString().StartsWith("/ip4/0.0.0.0/"))
            {
                addresses = ips
                    .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ip =>
                    {
                        return new MultiAddress(result.ToString().Replace("0.0.0.0", ip.Address.ToString()));
                    })
                    .ToArray();
            }
            else if (result.ToString().StartsWith("/ip6/::/"))
            {
                addresses = ips
                    .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    .Select(ip =>
                    {
                        return new MultiAddress(result.ToString().Replace("::", ip.Address.ToString()));
                    })
                    .ToArray();
            }
            else
            {
                addresses = new MultiAddress[] { result };
            }
            if (addresses.Count() == 0)
            {
                var msg = "Cannot determine address(es) for " + result;
                foreach (var ip in ips)
                {
                    msg += " nic-ip: " + ip.Address.ToString();
                }
                cancel.Cancel();
                throw new Exception(msg);
            }

            // Add actual addresses to listeners and local peer addresses.
            foreach (var a in addresses)
            {
                listeners.TryAdd(a, cancel);
            }
            LocalPeer.Addresses = LocalPeer
                .Addresses
                .Union(addresses)
                .ToArray();

            ListenerEstablished?.Invoke(this, LocalPeer);
            return Task.FromResult(addresses.First());
        }

        /// <summary>
        ///   Called when a remote peer is connecting to the local peer.
        /// </summary>
        /// <param name="stream">
        ///   The stream to the remote peer.
        /// </param>
        /// <param name="local">
        ///   The local peer's address.
        /// </param>
        /// <param name="remote">
        ///   The remote peer's address.
        /// </param>
        /// <remarks>
        ///   Establishes the protocols of the connection.
        /// </remarks>
        async void OnRemoteConnect(Stream stream, MultiAddress local, MultiAddress remote)
        {
            log.Debug("Got remote connection");
            log.Debug("local " + local);
            log.Debug("remote " + remote);

            // TODO: Check the policies

            var connection = new PeerConnection
            {
                IsIncoming = true,
                LocalPeer = LocalPeer,
                LocalAddress = local,
                LocalPeerKey = LocalPeerKey,
                RemoteAddress = remote,
                Stream = stream
            };

            // Mount the protocols.
            MountProtocols(connection);

            // Start the handshake
            // TODO: Isn't connection cancel token required.
            connection.ReadMessages(default(CancellationToken));

            // Wait for security to be established.
            await connection.SecurityEstablished.Task;
            // TODO: Maybe connection.LocalPeerKey = null;

            // Wait for the handshake to complete.
            var muxer = await connection.MuxerEstablished.Task;

            // Need details on the remote peer.
            Identify1 identify = null;
            lock (protocols)
            {
                identify = protocols.OfType<Identify1>().First();
            }
            connection.RemotePeer = await identify.GetRemotePeer(connection, default(CancellationToken));

            connection.RemotePeer = RegisterPeer(connection.RemotePeer);
            connection.RemoteAddress = new MultiAddress($"{remote}/ipfs/{connection.RemotePeer.Id}");
            connection.RemotePeer.ConnectedAddress = connection.RemoteAddress;

            var actual = Manager.Add(connection);
            if (actual == connection)
            {
                ConnectionEstablished?.Invoke(this, connection);
            }
        }

        /// <summary>
        ///   Add a protocol that is supported by the swarm.
        /// </summary>
        /// <param name="protocol">
        ///   The protocol to add.
        /// </param>
        public void AddProtocol(IPeerProtocol protocol)
        {
            lock (protocols)
            {
                protocols.Add(protocol);
            }
        }

        /// <summary>
        ///   Remove a protocol from the swarm.
        /// </summary>
        /// <param name="protocol">
        ///   The protocol to remove.
        /// </param>
        public void RemoveProtocol(IPeerProtocol protocol)
        {
            lock (protocols)
            {
                protocols.Remove(protocol);
            }
        }

        void MountProtocols(PeerConnection connection)
        {
            lock (protocols)
            {
                connection.AddProtocols(protocols);
            }
        }

        /// <summary>
        ///   Stop listening on the specified <see cref="MultiAddress"/>.
        /// </summary>
        /// <param name="address"></param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   Allows other peers to <see cref="ConnectAsync(MultiAddress, CancellationToken)">connect</see>
        ///   to the <paramref name="address"/>.
        ///   <para>
        ///   The addresses of the <see cref="LocalPeer"/> are updated.
        ///   </para>
        /// </remarks>
        public async Task StopListeningAsync(MultiAddress address)
        {
            if (!listeners.TryRemove(address, out CancellationTokenSource listener))
            {
                return;
            }

            try
            {
                if (!listener.IsCancellationRequested)
                {
                    listener.Cancel(false);

                    // Give some time away, so that cancel can run.
                    // TODO: Would be nice to make this deterministic.
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }

                // Remove any local peer address that depend on the cancellation token.
                var others = listeners
                    .Where(l => l.Value == listener)
                    .Select(l => l.Key)
                    .ToArray();

                LocalPeer.Addresses = LocalPeer.Addresses
                    .Where(a => a != address)
                    .Where(a => !others.Contains(a))
                    .ToArray();

                foreach (var other in others)
                {
                    listeners.TryRemove(other, out CancellationTokenSource _);
                }
            }
            catch (Exception e)
            {
                log.Error("stop listening failed", e);
            }
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
