using Common.Logging;
using Ipfs;
using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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

        /// <inheritdoc />
        public event EventHandler<PeerDiscoveredEventArgs> PeerDiscovered;

        /// <summary>
        ///   The listening addresses of the peer.
        /// </summary>
        /// <value>
        ///   Each address must end with the ipfs protocol and the public ID
        ///   of the peer.  For example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
        /// </value>
        /// <remarks>
        ///   This is typically <c>LocalPeer.Addresses</c>.
        /// </remarks>
        public IEnumerable<MultiAddress> Addresses { get; set; } = new List<MultiAddress>(0);

        /// <summary>
        ///   The service name for our peers.
        /// </summary>
        /// <value>
        ///   Defaults to "ipfs.local".
        /// </value>
        public string ServiceName { get; set; } = "ipfs.local";

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
            log.Debug("Starting");
            mdns = new MulticastService();
            mdns.NetworkInterfaceDiscovered += (s, e) => mdns.SendQuery(ServiceName);
            mdns.AnswerReceived += OnAnswerReceived;
            if (Broadcast)
            {
                mdns.QueryReceived += OnQueryReceived;
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
            var peerNames = msg.Answers
                .OfType<PTRRecord>()
                .Where(a => DnsObject.NamesEquals(a.Name, ServiceName))
                .Select(a => a.DomainName);
            foreach (var name in peerNames)
            {
                var id = name.Split('.')[0];
                var srv = msg.Answers
                    .OfType<SRVRecord>()
                    .First(r => DnsObject.NamesEquals(r.Name, name));
                var aRecords = msg.Answers
                    .OfType<ARecord>()
                    .Where(a => DnsObject.NamesEquals(a.Name, name) || DnsObject.NamesEquals(a.Name, srv.Target));
                foreach (var a in aRecords)
                {
                    OnPeerDiscovered(new PeerDiscoveredEventArgs
                    {
                        Address = new MultiAddress($"/ip4/{a.Address}/tcp/{srv.Port}/ipfs/{id}")
                    });
                }
                var aaaaRecords = msg.Answers
                    .OfType<AAAARecord>()
                    .Where(a => DnsObject.NamesEquals(a.Name, name) || DnsObject.NamesEquals(a.Name, srv.Target));
                foreach (var a in aaaaRecords)
                {
                    OnPeerDiscovered(new PeerDiscoveredEventArgs
                    {
                        Address = new MultiAddress($"/ip6/{a.Address}/tcp/{srv.Port}/ipfs/{id}")
                    });
                }
            }
        }

        void OnPeerDiscovered(PeerDiscoveredEventArgs e)
        {
            // Do not discover ourself.
            if (Addresses.Count() != 0)
            {
                var us = Addresses.First()
                    .Protocols
                    .Last(p => p.Name == "ipfs")
                    .Value;
                var them = e.Address
                    .Protocols
                    .Last(p => p.Name == "ipfs")
                    .Value;
                if (us == them)
                {
                    return;
                }
            }
            PeerDiscovered?.Invoke(this, e);
        }

        private void OnQueryReceived(object sender, MessageEventArgs e)
        {
            var msg = e.Message;
            if (!msg.Questions.Any(q => DnsObject.NamesEquals(q.Name, ServiceName)))
                return;
            if (Addresses.Count() == 0)
                return;

            var peerId = Addresses.First()
                .Protocols
                .Last(p => p.Name == "ipfs")
                .Value;
            var instanceName = $"{peerId}.{ServiceName}";
            var response = msg.CreateResponse();

            response.Answers.Add(new PTRRecord
            {
                Name = ServiceName,
                Class = Class.IN,
                DomainName = instanceName
            });

            response.Answers.Add(new SRVRecord
            {
                Name = instanceName,
                Target = instanceName,
                Port = Addresses
                    .SelectMany(a => a.Protocols)
                    .Where(p => p.Name == "tcp")
                    .Select(p => ushort.Parse(p.Value))
                    .First()
            });

            foreach (var a in Addresses.Where(a => a.Protocols[0].Name == "ip4").Select(a => a.Protocols[0]))
            {
                response.Answers.Add(new ARecord
                {
                    Name = instanceName,
                    Address = IPAddress.Parse(a.Value)
                });
            }

            foreach (var a in Addresses.Where(a => a.Protocols[0].Name == "ip6").Select(a => a.Protocols[0]))
            {
                response.Answers.Add(new AAAARecord
                {
                    Name = instanceName,
                    Address = IPAddress.Parse(a.Value)
                });
            }

            response.Answers.Add(new TXTRecord
            {
                Name = instanceName,
                Strings = { peerId }
            });

            mdns.SendAnswer(response);
        }


    }
}
