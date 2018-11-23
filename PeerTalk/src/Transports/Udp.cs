using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs;
using System.Net.Sockets;
using Common.Logging;
using System.Net;

namespace PeerTalk.Transports
{
    /// <summary>
    ///   Establishes a duplex stream between two peers
    ///   over UDP.
    /// </summary>
    public class Udp : IPeerTransport
    {
        static ILog log = LogManager.GetLogger(typeof(Udp));

        /// <inheritdoc />
        public async Task<Stream> ConnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            var port = address.Protocols
                .Where(p => p.Name == "udp")
                .Select(p => Int32.Parse(p.Value))
                .First();
            var ip = address.Protocols
                .Where(p => p.Name == "ip4" || p.Name == "ip6")
                .First();
            var socket = new Socket(
                ip.Name == "ip4" ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6,
                SocketType.Dgram,
                ProtocolType.Udp);

            // Handle cancellation of the connect attempt
            cancel.Register(() =>
            {
                socket.Dispose();
                socket = null;
            });

            try
            {
                log.Debug("connecting to " + address);
                await socket.ConnectAsync(ip.Value, port);
                log.Debug("connected " + address);
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // eat it, the caller has cancelled and doesn't care.
            }
            catch (Exception e)
            {
                log.Warn("failed " + address, e);
                throw;
            }
            if (cancel.IsCancellationRequested)
            {
                log.Debug("cancel " + address);
                socket?.Dispose();
                cancel.ThrowIfCancellationRequested();
            }
            return new DatagramStream(socket, ownsSocket: true);
        }

        /// <inheritdoc />
        public MultiAddress Listen(MultiAddress address, Action<Stream, MultiAddress, MultiAddress> handler, CancellationToken cancel)
        {
            var port = address.Protocols
                .Where(p => p.Name == "udp")
                .Select(p => Int32.Parse(p.Value))
                .FirstOrDefault();
            var ip = address.Protocols
                .Where(p => p.Name == "ip4" || p.Name == "ip6")
                .First();
            var ipAddress = IPAddress.Parse(ip.Value);
            var endPoint = new IPEndPoint(ipAddress, port);
            var socket = new Socket(
                endPoint.AddressFamily,
                SocketType.Dgram,
                ProtocolType.Udp);
            socket.Bind(endPoint);

            // If no port specified, then add it.
            var actualPort = ((IPEndPoint)socket.LocalEndPoint).Port;
            if (port != actualPort)
            {
                address = address.Clone();
                var protocol = address.Protocols.FirstOrDefault(p => p.Name == "udp");
                if (protocol != null)
                {
                    protocol.Value = actualPort.ToString();
                }
                else
                {
                    address.Protocols.AddRange(new MultiAddress("/udp/" + actualPort).Protocols);
                }
            }

            // TODO: UDP listener
            throw new NotImplementedException();
#if false
            var stream = new DatagramStream(socket, ownsSocket: true);
            handler(stream, address, null);

            return address;
#endif
        }

    }
}
