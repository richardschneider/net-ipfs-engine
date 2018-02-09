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
                if (socket != null)
                {
                    socket.Dispose();
                }
                return null;
            }
            return new NetworkStream(socket, ownsSocket: true);
        }

        /// <inheritdoc />
        public MultiAddress Listen(MultiAddress address, Action<Stream, MultiAddress, MultiAddress> handler, CancellationToken cancel)
        {
            var port = address.Protocols
                .Where(p => p.Name == "tcp")
                .Select(p => Int32.Parse(p.Value))
                .FirstOrDefault();
            var ip = address.Protocols
                .Where(p => p.Name == "ip4" || p.Name == "ip6")
                .First();
            var ipAddress = IPAddress.Parse(ip.Value);
            var endPoint = new IPEndPoint(ipAddress, port);
            var socket = new Socket(
                endPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);
            socket.Bind(endPoint);

            // If no port specified, then add it.
            var actualPort = ((IPEndPoint)socket.LocalEndPoint).Port;
            if (port != actualPort)
            {
                address = address.Clone();
                var protocol = address.Protocols.FirstOrDefault(p => p.Name == "tcp");
                if (protocol != null)
                {
                    protocol.Value = actualPort.ToString();
                }
                else
                {
                    address.Protocols.AddRange(new MultiAddress("/tcp/" + actualPort).Protocols);
                }
            }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => ProcessConnection(socket, address, handler, cancel));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return address;
        }

        void ProcessConnection(Socket socket, MultiAddress address, Action<Stream, MultiAddress, MultiAddress> handler, CancellationToken cancel)
        {
            log.Debug("listening on " + address);

            // Handle cancellation of the listener
            cancel.Register(() => 
            {
                socket.Dispose();
                socket = null;
            });

            socket.Listen(10);
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    Socket conn = socket.Accept();
                    var endPoint = (IPEndPoint)conn.RemoteEndPoint;
                    var s = new StringBuilder();
                    s.Append(endPoint.AddressFamily == AddressFamily.InterNetwork ? "/ip4/" : "/ip6/");
                    s.Append(endPoint.Address.ToString());
                    s.Append("/tcp/");
                    s.Append(endPoint.Port);
                    var remote = new MultiAddress(s.ToString());
                    log.Debug("connection from " + remote);
                    var peer = new NetworkStream(conn, ownsSocket: true);
                    try
                    {
                        handler(peer, address, remote);
                    }
                    catch (Exception e)
                    {
                        log.Error("listener handler failed " + address, e);
                        peer.Dispose();
                    }
                }
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // eat it
            }
            catch (Exception e)
            {
                log.Error("listener failed " + address, e);
                // eat it and give up
            }
            finally
            {
                if (socket != null)
                {
                    socket.Dispose();
                }
            }

            log.Debug("stop listening on " + address);
        }
    }
}
