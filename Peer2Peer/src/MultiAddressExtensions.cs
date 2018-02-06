using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer
{
    /// <summary>
    ///   Extensions to <see cref="MultiAddress"/>.
    /// </summary>
    public static class MultiAddressExtensions
    {
        static Dictionary<AddressFamily, string> supportedDnsAddressFamilies = new Dictionary<AddressFamily, string>
        {
            { AddressFamily.InterNetwork, "/ip4/" },
            { AddressFamily.InterNetworkV6, "/ip6/" },
        };

        /// <summary>
        ///   Creates a clone of the multiaddress.
        /// </summary>
        /// <param name="multiaddress">
        ///   The mutiaddress to clone.
        /// </param>
        /// <returns>
        ///   A new multiaddress with a copy of the <see cref="MultiAddress.Protocols"/>.
        /// </returns>
        public static MultiAddress Clone (this MultiAddress multiaddress)
        {
            var clone = new MultiAddress();
            clone.Protocols.AddRange(multiaddress.Protocols);

            return clone;
        }

        /// <summary>
        ///   The IP addresses for a host name.
        /// </summary>
        /// <param name="multiaddress">
        ///   The multiaddress to resolve.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is a sequence of possible multiaddresses.
        /// </returns>
        /// <exception cref="SocketException">
        ///   The host name cannot be resolved.
        /// </exception>
        /// <remarks>
        ///   When the <see cref="NetworkProtocol.Name"/> starts with "dns", then a DNS
        ///   lookup is performed to get all the IP addresses for the host name.  "dn4" and "dns6"
        ///   will filter the result for IPv4 and IPV6 addresses.
        /// </remarks>
        public static async Task<List<MultiAddress>> ResolveAsync(this MultiAddress multiaddress, CancellationToken cancel = default(CancellationToken))
        {
            var list = new List<MultiAddress>();
            var i = multiaddress.Protocols.FindIndex(ma => ma.Name.StartsWith("dns"));
            if (i < 0)
            {
                list.Add(multiaddress);
                return list;
            }

            var protocolName = multiaddress.Protocols[i].Name;
            var host = multiaddress.Protocols[i].Value;
            var addresses = (await Dns.GetHostAddressesAsync(host))
                .Where(a => supportedDnsAddressFamilies.ContainsKey(a.AddressFamily))
                .Where(a =>
                    protocolName == "dns" ||
                    protocolName == "dns4" && a.AddressFamily == AddressFamily.InterNetwork ||
                    protocolName == "dns6" && a.AddressFamily == AddressFamily.InterNetworkV6);
            foreach (var addr in addresses)
            {
                var ma0 = new MultiAddress(supportedDnsAddressFamilies[addr.AddressFamily] + addr.ToString());
                var ma1 = multiaddress.Clone();
                ma1.Protocols[i] = ma0.Protocols[0];
                list.Add(ma1);
            }

            return list;
        }
    }
}
