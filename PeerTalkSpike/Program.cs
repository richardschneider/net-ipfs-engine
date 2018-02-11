using Peer2Peer;
using Peer2Peer.Protocols;
using Peer2Peer.Transports;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace PeerTalkSpike
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            ClientConnect();
            //ServerListen();
        }
        
        static void ServerListen()
        {
            var localEndPoint = new IPEndPoint(IPAddress.Any, 4009);
            Socket listener = new Socket(localEndPoint.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);
            while (true)
            {
                Socket handler = listener.Accept();
                Console.WriteLine("Got a connection");
                var peer = new NetworkStream(handler);

                Message.WriteAsync("/multistream/1.0.0", peer).Wait();
                Message.ReadStringAsync(peer).Wait();

                while (true)
                {
                    var msg = Message.ReadStringAsync(peer).Result;
                    if (msg == "/mplex/6.7.4")
                    {
                        Message.WriteAsync("na", peer).Wait();
                    }
                    else
                    {
                        Message.WriteAsync(msg, peer).Wait();
                    }
                }
            }
        }

        static void ClientConnect()
        {
            var tcp = new Tcp();
            var peer = tcp.ConnectAsync("/ip4/127.0.0.1/tcp/4002").Result;

            var got = Message.ReadStringAsync(peer).Result;
            Message.WriteAsync("/multistream/1.0.0", peer).Wait();
            Message.WriteAsync("/plaintext/1.0.0", peer).Wait();
            while (true)
            {
                var msg = Message.ReadStringAsync(peer).Result;
                Message.WriteAsync(msg, peer).Wait();
            }
        }
    }
}
