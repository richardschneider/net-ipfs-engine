using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net.Sockets;
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
    }
}
