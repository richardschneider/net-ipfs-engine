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
        ///   The value is a function to create a instance of the peer protocol.
        /// </remarks>
        public static Dictionary<string, Func<IPeerProtocol>> Protocols;

        static ProtocolRegistry()
        {
            Protocols = new Dictionary<string, Func<IPeerProtocol>>();
            Register<Multistream1>();
            Register<Plaintext1>();
        }

        /// <summary>
        ///   Register a new protocol.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Register<T>() where T: IPeerProtocol, new()
        {
            var p = new T();
            Protocols.Add(p.ToString(), () => new T());
        }

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="protocolName"></param>
        /// <param name="Protocol"></param>
        public static void Register(string protocolName, Func<IPeerProtocol> Protocol)
        {
            Protocols.Add(protocolName, Protocol);
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
