using Ipfs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    /// <summary>
    ///   Extensions to <see cref="MultiAddress"/>.
    /// </summary>
    public static class MultiAddressExtensions
    {
        static Dictionary<AddressFamily, string> supportedDnsAddressFamilies = new Dictionary<AddressFamily, string>();
        static MultiAddress http = new MultiAddress("/tcp/80");
        static MultiAddress https = new MultiAddress("/tcp/443");

        static MultiAddressExtensions()
        {
            if (Socket.OSSupportsIPv4)
                supportedDnsAddressFamilies[AddressFamily.InterNetwork] = "/ip4/";
            if (Socket.OSSupportsIPv6)
                supportedDnsAddressFamilies[AddressFamily.InterNetworkV6] = "/ip6/";
        }

        /// <summary>
        ///   Determines if the multiaddress references
        ///   a loopback address.
        /// </summary>
        /// <param name="multiaddress">
        ///   The mutiaddress to clone.
        /// </param>
        /// <returns>
        ///   <b>true</b> for a loopback (127.0.0.1 or ::1).
        /// </returns>
        public static bool IsLoopback(this MultiAddress multiaddress)
        {
            return multiaddress.Protocols.Any(p =>
                p.Name == "ip4" && p.Value == "127.0.0.1" ||
                p.Name == "ip6" && p.Value == "::1");
        }

        /// <summary>
        ///   Get all the addresses for the specified <see cref="MultiAddress"/>.
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
        ///   <para>
        ///   When the <see cref="NetworkProtocol.Name"/> is "http" or "https", then
        ///   a "tcp/80" or "tcp/443" is respectively added.
        ///   </para>
        /// </remarks>
        public static async Task<List<MultiAddress>> ResolveAsync(this MultiAddress multiaddress, CancellationToken cancel = default(CancellationToken))
        {
            var list = new List<MultiAddress>();

            // HTTP
            var i = multiaddress.Protocols.FindIndex(ma => ma.Name == "http");
            if (i >= 0 && !multiaddress.Protocols.Any(p => p.Name == "tcp"))
            {
                multiaddress = multiaddress.Clone();
                multiaddress.Protocols.InsertRange(i + 1, http.Protocols);
            }

            // HTTPS
            i = multiaddress.Protocols.FindIndex(ma => ma.Name == "https");
            if (i >= 0 && !multiaddress.Protocols.Any(p => p.Name == "tcp"))
            {
                multiaddress = multiaddress.Clone();
                multiaddress.Protocols.InsertRange(i + 1, https.Protocols);
            }

            // DNS*
            i = multiaddress.Protocols.FindIndex(ma => ma.Name.StartsWith("dns"));
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
