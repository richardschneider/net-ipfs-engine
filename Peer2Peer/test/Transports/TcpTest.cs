using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2Peer.Transports
{
    
    [TestClass]
    public class TcpTest
    {
        [TestMethod]
        public void Connect_Unknown_Port()
        {
            var tcp = new Tcp();
            ExceptionAssert.Throws<SocketException>(() =>
            {
                var _ = tcp.ConnectAsync("/ip4/127.0.0.1/tcp/32700").Result;
            });
        }

        [TestMethod]
        public void Connect_Missing_TCP_Port()
        {
            var tcp = new Tcp();
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = tcp.ConnectAsync("/ip4/127.0.0.1/udp/32700").Result;
            });
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = tcp.ConnectAsync("/ip4/127.0.0.1").Result;
            });
        }

        [TestMethod]
        public void Connect_Missing_IP_Address()
        {
            var tcp = new Tcp();
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = tcp.ConnectAsync("/tcp/32700").Result;
            });
        }

        [TestMethod]
        public void Connect_Unknown_Address()
        {
            var tcp = new Tcp();
            ExceptionAssert.Throws<SocketException>(() =>
            {
                var _ = tcp.ConnectAsync("/ip4/127.0.10.10/tcp/32700").Result;
            });
        }

        [TestMethod]
        public async Task Connect_Cancelled()
        {
            var tcp = new Tcp();
            var cs = new CancellationTokenSource();
            cs.Cancel();
            var stream = await tcp.ConnectAsync("/ip4/127.0.10.10/tcp/32700", cs.Token);
            Assert.IsNull(stream);
        }

        [TestMethod]
        public async Task TimeProtocol()
        {
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var server = await new MultiAddress("/dns4/time.nist.gov/tcp/37").ResolveAsync(cs.Token);
            var data = new byte[4];

            var tcp = new Tcp();
            using (var time = await tcp.ConnectAsync(server[0], cs.Token))
            {
                var n = await time.ReadAsync(data, 0, data.Length, cs.Token);
                Assert.AreEqual(4, n);
            }
        }

        [TestMethod]
        public void Listen_Then_Cancel()
        {
            var tcp = new Tcp();
            var cs = new CancellationTokenSource();
            MultiAddress listenerAddress = null;
            Action<Stream, MultiAddress, MultiAddress> handler = (stream, local, remote) =>
            {
                Assert.Fail("handler should not be called");
            };
            listenerAddress = tcp.Listen("/ip4/127.0.0.1", handler, cs.Token);
            Assert.IsTrue(listenerAddress.Protocols.Any(p => p.Name == "tcp"));
            cs.Cancel();
        }

        [TestMethod]
        public async Task Listen()
        {
            var tcp = new Tcp();
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var connected = false;
            MultiAddress listenerAddress = null;
            Action<Stream, MultiAddress, MultiAddress> handler = (stream, local, remote) =>
            {
                Assert.IsNotNull(stream);
                Assert.AreEqual(listenerAddress, local);
                Assert.IsNotNull(remote);
                Assert.AreNotEqual(local, remote);
                connected = true;
            };
            try
            {
                listenerAddress = tcp.Listen("/ip4/127.0.0.1", handler, cs.Token);
                Assert.IsTrue(listenerAddress.Protocols.Any(p => p.Name == "tcp"));
                using (var stream = await tcp.ConnectAsync(listenerAddress, cs.Token))
                {
                    await Task.Delay(50);
                    Assert.IsNotNull(stream);
                    Assert.IsTrue(connected);
                }
            }
            finally
            {
                cs.Cancel();
            }
        }

        [TestMethod]
        public async Task Listen_Handler_Throws()
        {
            var tcp = new Tcp();
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var called = false;
            Action<Stream, MultiAddress, MultiAddress> handler = (stream, local, remote) =>
            {
                called = true;
                throw new Exception("foobar");
            };
            try
            {
                var addr = tcp.Listen("/ip4/127.0.0.1", handler, cs.Token);
                Assert.IsTrue(addr.Protocols.Any(p => p.Name == "tcp"));
                using (var stream = await tcp.ConnectAsync(addr, cs.Token))
                {
                    await Task.Delay(50);
                    Assert.IsNotNull(stream);
                    Assert.IsTrue(called);
                }
            }
            finally
            {
                cs.Cancel();
            }
        }

        [TestMethod]
        public async Task SendReceive()
        {
            var cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var tcp = new Tcp();
            using (var server = new HelloServer())
            using (var stream = await tcp.ConnectAsync(server.Address, cs.Token))
            {
                var bytes = new byte[5];
                await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.AreEqual("hello", Encoding.UTF8.GetString(bytes));
            }
        }

        class HelloServer : IDisposable
        {
            CancellationTokenSource cs = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            public HelloServer()
            {
                var tcp = new Tcp();
                Address = tcp.Listen("/ip4/127.0.0.1", Handler, cs.Token);
            }

            public MultiAddress Address { get; set; }

            public void Dispose()
            {
                cs.Cancel();
            }

            void Handler(Stream stream, MultiAddress local, MultiAddress remote)
            {
                var msg = Encoding.UTF8.GetBytes("hello");
                stream.Write(msg, 0, msg.Length);
                stream.Flush();
                stream.Dispose();
            }
        }
    }
}
