using Common.Logging;
using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.Discovery
{
    /// <summary>
    ///   Discovers the pre-configured peers.
    /// </summary>
    public class Bootstrap : IPeerDiscovery
    {
        static ILog log = LogManager.GetLogger(typeof(Bootstrap));

        /// <inheritdoc />
        public event EventHandler<PeerDiscoveredEventArgs> PeerDiscovered;

        /// <summary>
        ///   The addresses of the pre-configured peers.
        /// </summary>
        /// <value>
        ///   Each address must end with the ipfs protocol and the public ID
        ///   of the peer.  For example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
        /// </value>
        public IEnumerable<MultiAddress> Addresses { get; set; }

        /// <inheritdoc />
        public Task StartAsync()
        {
            log.Debug("Starting");
            if (Addresses == null)
            {
                log.Warn("No bootstrap addresses");
                return Task.CompletedTask;
            }
            foreach (var ma in Addresses)
            {
                try
                {
                    var _ = ma.PeerId;
                    OnPeerDiscovered(new PeerDiscoveredEventArgs { Address = ma });
                }
                catch (Exception e)
                {
                    log.Error(e);
                    continue; // silently ignore
                }
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            log.Debug("Stopping");
            PeerDiscovered = null;
            return Task.CompletedTask;
        }

        void OnPeerDiscovered(PeerDiscoveredEventArgs e)
        {
            PeerDiscovered?.Invoke(this, e);
        }

    }
}
