using Common.Logging;
using Ipfs;
using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PeerTalk.Discovery
{
    /// <summary>
    ///   Discovers peers using Multicast DNS.
    /// </summary>
    public class Mdns : IPeerDiscovery
    {
        static ILog log = LogManager.GetLogger(typeof(Mdns));
        MulticastService mdns;
        ServiceDiscovery discovery;
        ServiceProfile profile;

        /// <inheritdoc />
        public event EventHandler<PeerDiscoveredEventArgs> PeerDiscovered;

        /// <summary>
        ///  The local peer.
        /// </summary>
        public Peer LocalPeer { get; set; }

        /// <summary>
        ///   The service name for our peers.
        /// </summary>
        /// <value>
        ///   Defaults to "_ipfs._udp". _ipfs-discovery._udp.local
        /// </value>
        public string ServiceName { get; set; } = "_ipfs._udp";

        /// <summary>
        ///   Determines if the local peer responds to a query.
        /// </summary>
        /// <value>
        ///   <b>true</b> to answer queries.  Defaults to <b>true</b>.
        /// </value>
        public bool Broadcast { get; set; } = true;

        /// <summary>
        ///   Refresh state because the peer has change.
        /// </summary>
        /// <remarks>
        ///   Internal method to refresh the DNS-SD TXT record.
        /// </remarks>
        public void RefreshPeer()
        {
            // Refresh the TXT multiaddresses
            var nameServer = discovery.NameServer;
            Message query = new Message();
            query.Questions.Add(new Question
            {
                Name = profile.FullyQualifiedName,
                Type = DnsType.TXT
            }
            );
            var txt = nameServer.ResolveAsync(query)
                .Result
                .Answers.OfType<TXTRecord>()
                .First();
            txt.Strings = txt.Strings
                .Where(s => !s.StartsWith("dnsaddr="))
                .ToList();
            foreach (var address in LocalPeer.Addresses)
            {
                txt.Strings.Add($"dnsaddr={address.ToString()}");
            }

        }

        /// <inheritdoc />
        public Task StartAsync()
        {
            log.Debug("Starting");

            // The best spec is https://github.com/libp2p/libp2p/issues/28
            profile = new ServiceProfile(
                instanceName: LocalPeer.Id.ToBase58(),
                serviceName: ServiceName,
                port: 0
            );
            foreach (var address in LocalPeer.Addresses)
            {
                profile.AddProperty("dnsaddr", address.ToString());
            }

            mdns = new MulticastService();
            discovery = new ServiceDiscovery(mdns);
            mdns.NetworkInterfaceDiscovered += (s, e) =>
            {
                if (mdns == null)
                    return;
                try
                {
                    mdns.SendQuery(profile.QualifiedServiceName, type: DnsType.PTR);
                }
                catch (Exception ex)
                {
                    log.Debug("Failed to send query", ex);
                    // eat it
                }
            };
            mdns.AnswerReceived += OnAnswerReceived;
            if (Broadcast)
            {
                log.Debug($"Advertising {profile.FullyQualifiedName}");
                discovery.Advertise(profile);
            }
            mdns.Start();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            log.Debug("Stopping");
            if (mdns != null)
            {
                mdns.Stop();
                mdns = null;
            }
            PeerDiscovered = null;
            return Task.CompletedTask;
        }

        private void OnAnswerReceived(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            var peerServerNames = msg.Answers
                .OfType<PTRRecord>()
                .Where(a => DnsObject.NamesEquals(a.Name, profile.QualifiedServiceName))
                .Select(a => a.DomainName);
            foreach (var name in peerServerNames)
            {
                var addresses = msg.AdditionalRecords
                    .OfType<TXTRecord>()
                    .Where(t => t.Name == name)
                    .SelectMany(t => t.Strings)
                    .Where(s => s.StartsWith("dnsaddr="))
                    .Select(s => s.Substring(8))
                    .Select(s => new MultiAddress(s));
                foreach (var address in addresses)
                {
                    OnPeerDiscovered(new PeerDiscoveredEventArgs
                    {
                        Address = address
                    });
                }
            }
        }

        void OnPeerDiscovered(PeerDiscoveredEventArgs e)
        {
            // Do not discover ourself.
            var us = LocalPeer.Id;
            var them = e.Address.PeerId;
            if (us != them)
            {
                PeerDiscovered?.Invoke(this, e);
            }
        }
    }
}
