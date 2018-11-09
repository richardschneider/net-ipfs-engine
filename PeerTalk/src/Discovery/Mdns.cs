using Common.Logging;
using Ipfs;
using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

namespace PeerTalk.Discovery
{
    /// <summary>
    ///   Base class to discover peers using Multicast DNS.
    /// </summary>
    public abstract class Mdns : IPeerDiscovery
    {
        static ILog log = LogManager.GetLogger(typeof(Mdns));

        /// <inheritdoc />
        public event EventHandler<PeerDiscoveredEventArgs> PeerDiscovered;

        /// <summary>
        ///  The local peer.
        /// </summary>
        public Peer LocalPeer { get; set; }

        /// <summary>
        ///   The Muticast Domain Name Service to use.
        /// </summary>
        public MulticastService MulticastService { get; set; }

        /// <summary>
        ///   The service name for our peers.
        /// </summary>
        /// <value>
        ///   Defaults to "ipfs".
        /// </value>
        public string ServiceName { get; set; } = "ipfs";

        /// <summary>
        ///   Determines if the local peer responds to a query.
        /// </summary>
        /// <value>
        ///   <b>true</b> to answer queries.  Defaults to <b>true</b>.
        /// </value>
        public bool Broadcast { get; set; } = true;

        /// <inheritdoc />
        public Task StartAsync()
        {
            MulticastService.NetworkInterfaceDiscovered += (s, e) =>
            {
                try
                {
                    var profile = BuildProfile();
                    var discovery = new ServiceDiscovery(MulticastService);
                    OnServiceDiscovery(discovery);
                    discovery.ServiceInstanceDiscovered += OnServiceInstanceDiscovered;

                    if (Broadcast && profile != null)
                    {
                        log.Debug($"Advertising {profile.FullyQualifiedName}");
                        discovery.Advertise(profile);
                    }

                    // Ask all peers to broadcast discovery info.
                    discovery.QueryServiceInstances(ServiceName);
                }
                catch (Exception ex)
                {
                    log.Debug("Failed to send query", ex);
                    // eat it
                }
            };

            return Task.CompletedTask;
        }


        /// <inheritdoc />
        public Task StopAsync()
        {
            PeerDiscovered = null;
            return Task.CompletedTask;
        }

        void OnServiceInstanceDiscovered(object sender, ServiceInstanceDiscoveryEventArgs e)
        {
            var msg = e.Message;

            // Is it our service?
            var qsn = ServiceName + ".local";
            if (!e.ServiceInstanceName.EndsWith(qsn))
                return;

            foreach (var address in GetAddresses(msg))
            {
                if (LocalPeer.Id == address.PeerId)
                {
                    continue;
                }
                PeerDiscovered?.Invoke(this, new PeerDiscoveredEventArgs { Address = address });
            }
        }
    
        /// <summary>
        ///   Build the profile which contains the DNS records that are needed
        ///   to locate and connect to the local peer.
        /// </summary>
        /// <returns>
        ///   Describes the service.
        /// </returns>
        public abstract ServiceProfile BuildProfile();

        /// <summary>
        ///   Get the addresses of the peer in the DNS message.
        /// </summary>
        /// <param name="message">
        ///   An answer describing a peer.
        /// </param>
        /// <returns>
        ///   All the addresses of the peer.
        /// </returns>
        public abstract IEnumerable<MultiAddress> GetAddresses(Message message);

        /// <summary>
        ///   Allows derived class to modify the service discovery behavior.
        /// </summary>
        /// <param name="discovery"></param>
        protected virtual void OnServiceDiscovery(ServiceDiscovery discovery)
        {
        }
    }
}
