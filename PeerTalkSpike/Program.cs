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

            //ClientConnect();
            ServerListen();
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

                var x = "/multistream/1.0.0\n";
                var r = new byte[x.Length + 1];
                r[0] = (byte)x.Length;
                x.ToCharArray().Select(c => (byte)c).ToArray().CopyTo(r, 1);
                handler.Send(r);
                Console.Write("sent " + x);

                int bytes = 0;
                var buffer = new byte[1];
                do
                {
                    bytes = handler.Receive(buffer);
                    Console.WriteLine(string.Format("got byte 0x{0:x2} '{1}'", buffer[0], (char)buffer[0]));
                } while (bytes != 0);

            }
        }

        static void ClientConnect()
        { 
            var peer = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            peer.Connect("127.0.0.1", 4002);

            int bytes = 0;
            var buffer = new byte[1];
            do
            {
                bytes = peer.Receive(buffer);
                Console.WriteLine(string.Format("got byte 0x{0:x2} '{1}'", buffer[0], (char)buffer[0]));
                if (buffer[0] == 0x0a)
                {
                    var x = "/plaintext/1.0.0\n";
                    var r = new byte[x.Length + 1];
                    r[0] = (byte)x.Length;
                    x.ToCharArray().Select(c => (byte)c).ToArray().CopyTo(r, 1);
                    peer.Send(r);
                    Console.Write("sent " + x);
                }
            } while (bytes != 0);
        }
    }
}
