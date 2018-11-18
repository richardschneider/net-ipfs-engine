using Common.Logging;
using Ipfs;
using PeerTalk;
using PeerTalk.Protocols;
using ProtoBuf;
using Semver;
using System;
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

        public RoutingTable RoutingTable = new RoutingTable();

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

            RoutingTable = new RoutingTable();
            Swarm.OtherProtocols.Add(this);
            Swarm.PeerDiscovered += Swarm_PeerDiscovered;
            foreach (var peer in Swarm.KnownPeers)
            {
                RoutingTable.Peers.Add(peer);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            log.Debug("Stopping");

            Swarm.OtherProtocols.Remove(this);
            Swarm.PeerDiscovered -= Swarm_PeerDiscovered;

            return Task.CompletedTask;
        }

        /// <summary>
        ///   The swarm has discovered a new peer, update the routing table.
        /// </summary>
        void Swarm_PeerDiscovered(object sender, Peer e)
        {
            RoutingTable.Peers.Add(e);
        }

        /// <inheritdoc />
        public async Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default(CancellationToken))
        {
            // Can always find self.
            if (Swarm.LocalPeer.Id == id)
                return Swarm.LocalPeer;

            // Maybe the swarm knows about it.
            var found = Swarm.KnownPeers.FirstOrDefault(p => p.Id == id);
            if (found != null)
                return found;

            // Ask our peers for information of requested peer.
            var nearest = RoutingTable.NearestPeers(id);
            var query = new DhtMessage
            {
                Type = MessageType.FindNode,
                Key = id.ToArray()
            };
            log.Debug($"Query {query.Type}");
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
                        var closerPeer = Swarm.RegisterPeer(closer.ToPeer());

                        if (id == closerPeer.Id)
                        {
                            log.Debug("Found answer");
                            return closerPeer;
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
        public Task<IEnumerable<Peer>> FindProvidersAsync(Cid id, int limit = 20, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException("DHT FindProvidersAsync");
        }
    }
}
