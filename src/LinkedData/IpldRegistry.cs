using Ipfs;
using Ipfs.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine.LinkedData
{
    /// <summary>
    ///   Metadata on <see cref="ILinkedDataFormat"/>.
    /// </summary>
    public static class IpldRegistry
    {
        /// <summary>
        ///   All the supported IPLD formats.
        /// </summary>
        /// <remarks>
        ///   The key is the multicodec name.
        ///   The value is an object that implements <see cref="ILinkedDataFormat"/>.
        /// </remarks>
        public static Dictionary<string, ILinkedDataFormat> Formats;

        static IpldRegistry()
        {
            Formats = new Dictionary<string, ILinkedDataFormat>();
            Register<CborFormat>("dag-cbor");
            Register<CborFormat>("cbor");
            Register<ProtobufFormat>("dag-pb");
            Register<RawFormat>("raw");
        }

        /// <summary>
        ///   Register a new IPLD format.
        /// </summary>
        /// <typeparam name="T">
        ///   A Type that implements <see cref="ILinkedDataFormat"/>.
        /// </typeparam>
        /// <param name="name">
        ///   The multicodec name.
        /// </param>
        public static void Register<T>(string name) where T : ILinkedDataFormat, new()
        {
            Formats.Add(name, new T());
        }

        /// <summary>
        ///   Remove the IPLD format.
        /// </summary>
        /// <param name="name">
        ///   The multicodec name.
        /// </param>
        public static void Deregister(string name)
        {
            Formats.Remove(name);
        }

    }
}
