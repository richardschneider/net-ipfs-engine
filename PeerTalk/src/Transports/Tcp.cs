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
#if !NET461
using System.Runtime.InteropServices;
#endif

namespace PeerTalk.Transports
{
    /// <summary>
    ///   Establishes a duplex stream between two peers
    ///   over TCP.
    /// </summary>
    /// <remarks>
    ///   <see cref="ConnectAsync"/> determines the network latency and sets the timeout
    ///   to 3 times the latency or <see cref="MinReadTimeout"/>.
    /// </remarks>
    public class Tcp : IPeerTransport
    {
        static ILog log = LogManager.GetLogger(typeof(Tcp));

        /// <summary>
        ///  The minimum read timeout.
        /// </summary>
        /// <value>
        ///   Defaults to 3 seconds.
        /// </value>
        public static TimeSpan MinReadTimeout = TimeSpan.FromSeconds(3);

        /// <inheritdoc />
        public async Task<Stream> ConnectAsync(MultiAddress address, CancellationToken cancel = default(CancellationToken))
        {
            var port = address.Protocols
                .Where(p => p.Name == "tcp")
                .Select(p => Int32.Parse(p.Value))
                .First();
            var ip = address.Protocols
                .Where(p => p.Name == "ip4" || p.Name == "ip6")
                .FirstOrDefault();
            if (ip == null)
                throw new ArgumentException($"Missing IP address in '{address}'.", "address");
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

            TimeSpan latency = MinReadTimeout; // keep compiler happy
            try
            {
                log.Debug("connecting to " + address);
                var start = DateTime.Now;
                await socket.ConnectAsync(ip.Value, port);
                latency = DateTime.Now - start;
                log.Debug($"connected to {address} in {latency.TotalMilliseconds} ms");
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // eat it, the caller has cancelled and doesn't care.
            }
            catch (Exception e)
            {
                log.Warn("failed " + address, e);
                if (socket != null)
                {
                    socket.Dispose();
                }
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

            var timeout = (int) Math.Max(MinReadTimeout.TotalMilliseconds, latency.TotalMilliseconds * 3);
            socket.LingerState = new LingerOption(false, 0);
            socket.ReceiveTimeout = timeout;
            socket.SendTimeout = timeout;
            var stream =  new NetworkStream(socket, ownsSocket: true);
            stream.ReadTimeout = timeout;
            stream.WriteTimeout = timeout;

#if NETSTANDARD14
            return stream;
#else
            return stream;
            return new BufferedStream(stream);
#endif
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
                .FirstOrDefault();
            if (ip == null)
                throw new ArgumentException($"Missing IP address in '{address}'.", "address");
            var ipAddress = IPAddress.Parse(ip.Value);
            var endPoint = new IPEndPoint(ipAddress, port);
            var socket = new Socket(
                endPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);
            try
            {
                socket.Bind(endPoint);
                socket.Listen(1);
            }
            catch (Exception e)
            {
                socket.Dispose();
                throw new Exception("Bind/listen failed on " + address, e);
            }

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
                // .Net Standard on Unix neeeds this to cancel the Accept
#if !NET461
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
#endif
                socket.Dispose();
                socket = null;
            });

            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    Socket conn = socket.Accept();
                    if (conn == null) 
                    {
                        log.Warn("Null socket from Accept");
                        continue;
                    }
                    MultiAddress remote = null;
                    var endPoint = conn.RemoteEndPoint as IPEndPoint;
                    if (endPoint != null)
                    {
                        var s = new StringBuilder();
                        s.Append(endPoint.AddressFamily == AddressFamily.InterNetwork ? "/ip4/" : "/ip6/");
                        s.Append(endPoint.Address.ToString());
                        s.Append("/tcp/");
                        s.Append(endPoint.Port);
                        remote = new MultiAddress(s.ToString());
                        log.Debug("connection from " + remote);
                    }

                    conn.NoDelay = true;
                    Stream peer = new NetworkStream(conn, ownsSocket: true);
                    peer.WriteTimeout = 1000;
                    peer.ReadTimeout = 1000;
#if !NETSTANDARD14
                    // BufferedStream not available in .Net Standard 1.4
                    peer = new BufferedStream(peer);
#endif
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
                Console.WriteLine(e.Message);
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
