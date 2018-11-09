using Common.Logging;
using Ipfs;
using Makaretu.Dns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PeerTalk.Discovery
{
    /// <summary>
    ///   Discovers peers using Multicast DNS according to
    ///   go-ipfs v0.4.17
    /// </summary>
    /// <remarks>
    ///   GO peers are not using the mDNS multicast address (224.0.0.251)
    ///   <see href="https://github.com/libp2p/go-libp2p/issues/469"/>.
    ///   Basically this cannot work until the issue is resolved.
    /// </remarks>
    public class MdnsGo : MdnsJs
    {
        /// <summary>
        ///   MDNS go is the same as MdnsJs except that the
        ///   service name is "_ipfs-discovery._udp".
        /// </summary>
        public MdnsGo()
        {
            ServiceName = "_ipfs-discovery._udp";
        }

    }
}
