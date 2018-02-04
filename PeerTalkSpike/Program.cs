using Peer2Peer;
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
        
        static byte[] EncodeMessage(string msg)
        {
            var bytes = new byte[msg.Length + 2];
            bytes[0] = (byte)(msg.Length + 1);
            bytes[bytes.Length - 1] = (byte)'\n';
            msg.ToCharArray().Select(c => (byte)c).ToArray().CopyTo(bytes, 1);
            return bytes;
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
            var socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            socket.Connect("127.0.0.1", 4002);
            var peer = new NetworkStream(socket);

            var got = Message.ReadString(peer);
            Message.Write("/multistream/1.0.0", peer);
            Message.Write("ls", peer);
            while (true)
            {
                got = Message.ReadString(peer);
            }
        }
    }
}
