using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerTalk.Cryptography
{
    /// <summary>
    ///   A short term key on a curve.
    /// </summary>
    /// <remarks>
    ///   Ephemeral keys are different from other keys in IPFS; they are NOT
    ///   protobuf encoded and are NOT self describing.  The encoding is an
    ///   uncompressed ECPoint; the first byte s a 4 and followed by X and Y co-ordinates.
    ///   <para>
    ///   It as assummed that the curve name is known a priori.
    ///   </para>
    /// </remarks>
    public class EphermalKey
    {
        ECPublicKeyParameters publicKey;
        AsymmetricKeyParameter privateKey;

        /// <summary>
        ///   Gets the IPFS encoding of the public key.
        /// </summary>
        /// <returns>
        ///   Returns the uncompressed EC point.
        /// </returns>
        public byte[] PublicKeyBytes()
        {
            return publicKey.Q.GetEncoded(compressed: false);
        }

        /// <summary>
        ///   Create a shared secret between this key and another.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public byte[] GenerateSharedSecret(EphermalKey other)
        {

        }

        /// <summary>
        ///   Create a public key from the IPFS ephermal encoding.
        /// </summary>
        /// <param name="bytes">
        ///   The IPFS encoded ephermal key.
        /// </param>
        public static EphermalKey CreatePublicKeyFromIpfs(string curveName, byte[] bytes)
        {
            X9ECParameters ecP = ECNamedCurveTable.GetByName(curveName);
            if (ecP == null)
                throw new Exception($"Unknown curve name '{curveName}'.");
            var domain = new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H, ecP.GetSeed());
            var q = ecP.Curve.DecodePoint(bytes);
            return new EphermalKey
            {
                publicKey = new ECPublicKeyParameters(q, domain)
            };
        }


        public static EphermalKey Generate(string curveName)
        {
            X9ECParameters ecP = ECNamedCurveTable.GetByName(curveName);
            if (ecP == null)
                throw new Exception($"Unknown curve name '{curveName}'.");
            var domain = new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H, ecP.GetSeed());
            var g = GeneratorUtilities.GetKeyPairGenerator("EC");
            g.Init(new ECKeyGenerationParameters(domain, new SecureRandom()));
            var keyPair = g.GenerateKeyPair();

            return new EphermalKey
            {
                privateKey = keyPair.Private,
                publicKey = (ECPublicKeyParameters)keyPair.Public
            };
        }

    }
}
