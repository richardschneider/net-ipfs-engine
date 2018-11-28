using Common.Logging;
using Ipfs;
using PeerTalk;
using PeerTalk.Protocols;
using ProtoBuf;
using Semver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace PeerTalk.Routing
{
    /// <summary>
    ///   DHT Protocol version 1.0
    /// </summary>
    public class Dht1 : IPeerProtocol, IService, IPeerRouting, IContentRouting
    {
        static ILog log = LogManager.GetLogger(typeof(Dht1));

        /// <inheritdoc />
        public string Name { get; } = "ipfs/kad";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1, 0);

        /// <summary>
        ///   Provides access to other peers.
        /// </summary>
        public Swarm Swarm { get; set; }

        /// <summary>
        ///  Routing information on peers.
        /// </summary>
        public RoutingTable RoutingTable;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"/{Name}/{Version}";
        }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection, Stream stream, CancellationToken cancel = default(CancellationToken))
        {
            var request = await ProtoBufHelper.ReadMessageAsync<DhtMessage>(stream, cancel);

            log.Debug($"got message from {connection.RemotePeer}");
            // TODO: process the request
        }

        /// <inheritdoc />
        public Task StartAsync()
        {
            log.Debug("Starting");

            RoutingTable = new RoutingTable(Swarm.LocalPeer);
            Swarm.AddProtocol(this);
            Swarm.PeerDiscovered += Swarm_PeerDiscovered;
            foreach (var peer in Swarm.KnownPeers)
            {
                RoutingTable.Add(peer);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            log.Debug("Stopping");

            Swarm.RemoveProtocol(this);
            Swarm.PeerDiscovered -= Swarm_PeerDiscovered;

            return Task.CompletedTask;
        }

        /// <summary>
        ///   The swarm has discovered a new peer, update the routing table.
        /// </summary>
        void Swarm_PeerDiscovered(object sender, Peer e)
        {
            RoutingTable.Add(e);
        }

        /// <inheritdoc />
        public async Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default(CancellationToken))
        {
            // Can always find self.
            if (Swarm.LocalPeer.Id == id)
                return Swarm.LocalPeer;

            // Maybe the swarm knows about it.
            var found = Swarm.KnownPeers.FirstOrDefault(p => p.Id == id);
            if (found != null && found.Addresses.Count() > 0)
                return found;

            // Ask our peers for information of requested peer.
            var nearest = RoutingTable.NearestPeers(id);
            var query = new DhtMessage
            {
                Type = MessageType.FindNode,
                Key = id.ToArray()
            };
            log.Debug($"Query {query.Type} {id}");
            foreach (var peer in nearest)
            {
                log.Debug($"Query peer {peer.Id} for {query.Type}");

                using (var stream = await Swarm.DialAsync(peer, this.ToString(), cancel))
                {
                    ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, query, PrefixStyle.Base128);
                    await stream.FlushAsync(cancel);
                    var response = await ProtoBufHelper.ReadMessageAsync<DhtMessage>(stream, cancel);
                    if (response.CloserPeers == null)
                    {
                        continue;
                    }
                    foreach (var closer in response.CloserPeers)
                    {
                        if (closer.TryToPeer(out Peer p))
                        {
                            p = Swarm.RegisterPeer(p);
                            if (id == p.Id)
                            {
                                log.Debug($"Found answer for {id}");
                                return p;
                            }
                        }
                    }
                }
            }

            // Unknown peer ID.
            throw new KeyNotFoundException($"Cannot locate peer '{id}'.");
        }

        /// <inheritdoc />
        public Task ProvideAsync(Cid cid, bool advertise = true, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException("DHT ProvideAsync");
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Peer>> FindProvidersAsync(
            Cid id, 
            int limit = 20, 
            Action<Peer> action = null,
            CancellationToken cancel = default(CancellationToken))
        {
            log.Debug($"Find providers for {id}");

            var providers = new List<Peer>();
            var visited = new List<Peer> { Swarm.LocalPeer };
            var key = id.Hash.ToArray();

            var query = new DhtMessage
            {
                Type = MessageType.GetProviders,
                Key = key
            };

            while (!cancel.IsCancellationRequested)
            {
                if (providers.Count >= limit)
                    break;

                // Get the nearest peers that have not been visited.
                var peers = RoutingTable
                    .NearestPeers(id.Hash)
                    .Where(p => !visited.Contains(p))
                    .Take(3)
                    .ToArray();
                if (peers.Length == 0)
                    break;

                visited.AddRange(peers);

                try
                {
                    log.Debug($"Next {peers.Length} queries");
                    // Only allow 10 seconds per pass.
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancel))
                    {
                        var tasks = peers.Select(p => FindProvidersAsync(p, id, query, providers, action, limit, cts));
                        await Task.WhenAll(tasks);
                    }
                }
                catch (Exception e)
                {
                    log.Warn("dquery failed " + e.Message); //eat it
                }
            }

            // All peers queried or the limit has been reached.
            log.Debug($"Found {providers.Count} providers for {id}, visited {visited.Count} peers");
            return providers.Take(limit);
        }

        /// <summary>
        ///   Ask a peer for provider peers.
        /// </summary>
        async Task FindProvidersAsync(
            Peer peer,
            Cid id,
            DhtMessage query,
            List<Peer> providers,
            Action<Peer> action,
            int limit,
            CancellationTokenSource cts)
        {
            try
            {
                var cancel = cts.Token;

                using (var stream = await Swarm.DialAsync(peer, this.ToString(), cancel))
                {
                    // Send the KAD query and get a response.
                    ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, query, PrefixStyle.Base128);
                    await stream.FlushAsync(cancel);
                    var response = await ProtoBufHelper.ReadMessageAsync<DhtMessage>(stream, cancel);

                    if (response.ProviderPeers != null)
                    {
                        foreach (var provider in response.ProviderPeers)
                        {
                            if (cancel.IsCancellationRequested)
                                break;
                            if (provider.TryToPeer(out Peer p))
                            {
                                p = Swarm.RegisterPeer(p);
                                // Only unique answers
                                if (!providers.Contains(p))
                                {
                                    providers.Add(p);
                                    action?.Invoke(p);
                                }
                            }
                        }
                    }
                    // If enough answers, then cancel the query.
                    if (providers.Count >= limit && !cts.IsCancellationRequested)
                    {
                        log.Debug($"Required answers ({limit}) reached");
                        cts.Cancel(false);
                    }

                    // Process the closer peers.
                    if (response.CloserPeers != null)
                    {
                        foreach (var closer in response.CloserPeers)
                        {
                            if (cancel.IsCancellationRequested)
                                break;
                            if (closer.TryToPeer(out Peer p))
                            {
                                Swarm.RegisterPeer(p);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Warn("query failed " + e.Message); // eat it. Hopefully other peers will provide an answet.
            }
        }
    }
}
