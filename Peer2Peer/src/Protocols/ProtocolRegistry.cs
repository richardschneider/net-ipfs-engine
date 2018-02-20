using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peer2Peer.Protocols
{
    /// <summary>
    ///   Metadata on <see cref="IPeerProtocol"/>.
    /// </summary>
    public static class ProtocolRegistry
    {
        /// <summary>
        ///   All the peer protocols.
        /// </summary>
        /// <remarks>
        ///   The key is the name and version of the peer protocol, like "/multiselect/1.0.0".
        ///   The value is an instance of the peer protocol.
        /// </remarks>
        public static Dictionary<string, IPeerProtocol> Protocols;

        static ProtocolRegistry()
        {
            Protocols = new Dictionary<string, IPeerProtocol>();
            Register<Multistream1>();
            Register<Plaintext1>();
            Register<Identify1>();
        }

        /// <summary>
        ///   Register a new protocol.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Register<T>() where T: IPeerProtocol, new()
        {
            var p = new T();
            Protocols.Add(p.ToString(), p);
        }

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="protocol"></param>
        public static void Register(IPeerProtocol protocol)
        {
            Protocols.Add(protocol.ToString(), protocol);
        }

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="protocolName"></param>
        public static void Deregister(string protocolName)
        {
            Protocols.Remove(protocolName);
        }

    }
}
