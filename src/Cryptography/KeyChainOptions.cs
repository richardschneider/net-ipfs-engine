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
    /// <seealso cref="IpfsEngine.Options"/>
    public class KeyChainOptions
    {
        /// <summary>
        ///   The default key type, when generating a key.
        /// </summary>
        /// <value>
        ///   "rsa", "ed25519" or "secp256k1". Defaults to "rsa".
        /// </value>
        public string DefaultKeyType { get; set; } = "rsa";

        /// <summary>
        ///   The default key size, when generating a RSA key.
        /// </summary>
        /// <value>
        ///   The size in bits.  Defaults to 2048.
        /// </value>
        public int DefaultKeySize { get; set; } = 2048;

        /// <summary>
        ///   The defaults for the derived encryption key.
        /// </summary>
        /// <value>
        ///   The options to generated a DEK.
        /// </value>
        /// <remarks>
        ///   The derived encryption key is used to store the encrypted keys.
        ///   Theses options can not change once the key chain is created.
        /// </remarks>
        public KeyChainDekOptions Dek { get; set; } = new KeyChainDekOptions();
    }

    /// <summary>
    ///   Options to generate the derived encryption key.
    /// </summary>
    /// <seealso href="https://cryptosense.com/parameter-choice-for-pbkdf2/"/>
    public class KeyChainDekOptions
    {
        /// <summary>
        ///    The desired length of the derived key
        /// </summary>
        /// <value>
        ///   The length in bytes.  Defaults to 512.
        /// </value>
        public int KeyLength { get; set; } = 512 / 8;

        /// <summary>
        ///   The number of iterations desired
        /// </summary>
        /// <value>
        ///   Defaults to 10,000.
        /// </value>
        public int IterationCount { get; set; } =  10 * 1000;

        /// <summary>
        ///   Some random data for the <see cref="Hash"/>.
        /// </summary>
        /// <seealso href="https://en.wikipedia.org/wiki/Salt_(cryptography)"/>
        public string Salt { get; set; } = "at least 16 characters long";

        /// <summary>
        ///   The name of the hashing function.
        /// </summary>
        /// <value>
        ///   One of the known <see cref="Ipfs.Registry.HashingAlgorithm"/> names. Defaults to "sha2-512".
        /// </value>
        public string Hash { get; set; } = "sha2-512";
  }
}

