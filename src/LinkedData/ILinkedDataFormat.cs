using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine.LinkedData
{
    /// <summary>
    ///   A specific format for linked data.
    /// </summary>
    /// <remarks>
    ///   Allows the conversion between the canonincal form of linked data and its binary
    ///   representation in a specific format.
    ///   <para>
    ///   The canonical form is a <see cref="CBORObject"/>.
    ///   </para>
    /// </remarks>
    public interface ILinkedDataFormat
    {
        /// <summary>
        ///   Convert the binary represention into the equivalent canonical form. 
        /// </summary>
        /// <param name="data">
        ///   The linked data encoded in a specific format.
        /// </param>
        /// <returns>
        ///   The canonical representation of the <paramref name="data"/>.
        /// </returns>
        CBORObject Deserialise(byte[] data);

        /// <summary>
        ///   Convert the canonical data into the specific format.
        /// </summary>
        /// <param name="data">
        ///   The canonical data to convert.
        /// </param>
        /// <returns>
        ///   The binary representation of the <paramref name="data"/> encoded
        ///   in the specific format.
        /// </returns>
        byte[] Serialize(CBORObject data);
    }
}
