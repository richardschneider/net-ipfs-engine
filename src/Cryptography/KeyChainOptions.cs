using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Engine.Cryptography
{
    /// <summary>
    ///   Configuration options for the <see cref="KeyChain"/>.
    /// </summary>
    public class KeyChainOptions
    {
        /// <summary>
        ///   The default key type, when generating a key.
        /// </summary>
        /// <value>
        ///   Defaults to "rsa".
        /// </value>
        public string DefaultKeyType { get; set; } = "rsa";

        /// <summary>
        ///   The default key size, when generating a RSA key.
        /// </summary>
        /// <value>
        ///   The size in bits.  Defaults to 2048.
        /// </value>
        public int DefaultKeySize { get; set; } = 2048;
    }
}
