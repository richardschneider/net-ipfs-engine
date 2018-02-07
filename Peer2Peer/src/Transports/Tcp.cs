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

namespace Peer2Peer.Transports
{
    /// <summary>
    ///   Establishes a duplex stream between two peers
    ///   over TCP.
    /// </summary>
    public class Tcp : IPeerTransport
    {
        static ILog log = LogManager.GetLogger(typeof(Tcp));

        /// <inheritdoc />
        public async Task<Stream> ConnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            var port = address.Protocols
                .Where(p => p.Name == "tcp")
                .Select(p => Int32.Parse(p.Value))
                .First();
            var ip = address.Protocols
                .Where(p => p.Name == "ip4" || p.Name == "ip6")
                .First();
            var socket = new Socket(
                ip.Name == "ip4" ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6,
                SocketType.Stream,
                ProtocolType.Tcp);

            // Handle cancellation of the connect attempt
            cancel.Register(() => socket.Dispose());

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
                socket.Dispose();
                return null;
            }
            return new NetworkStream(socket);
        }
    }
}
