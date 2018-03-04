using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.Transports
{
    static class TransportRegistry
    {
        public static Dictionary<string, Func<IPeerTransport>> Transports;

        static TransportRegistry()
        {
            Transports = new Dictionary<string, Func<IPeerTransport>>();
            Register("tcp", () => new Tcp());
            Register("udp", () => new Udp());
        }

        public static void Register(string protocolName, Func<IPeerTransport> transport)
        {
            Transports.Add(protocolName, transport);
        }

        public static void Deregister(string protocolName)
        {
            Transports.Remove(protocolName);
        }

    }
}
