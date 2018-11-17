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

            Swarm.OtherProtocols.Add(this);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            log.Debug("Stopping");

            Swarm.OtherProtocols.Remove(this);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default(CancellationToken))
        {
            // Can always find self.
            if (Swarm.LocalPeer.Id == id)
                return Task.FromResult(Swarm.LocalPeer);

            // Maybe the swarm knows about it.
            var found = Swarm.KnownPeers.FirstOrDefault(p => p.Id == id);
            if (found != null)
                return Task.FromResult(found);

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
