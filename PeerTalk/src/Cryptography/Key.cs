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
    ///   An asymmetric key.
    /// </summary>
    public class Key
    {
        const string RsaSigningAlgorithmName = "SHA-256withRSA";
        const string EcSigningAlgorithmName = "SHA-256withECDSA";

        AsymmetricKeyParameter publicKey;
        AsymmetricKeyParameter privateKey;
        string signingAlgorithmName;

        private Key()
        {

        }

        /// <summary>
        ///   Verify that signature matches the data.
        /// </summary>
        /// <param name="data">
        ///   The data to check.
        /// </param>
        /// <param name="signature">
        ///   The supplied signature of the <paramref name="data"/>.
        /// </param>
        /// <exception cref="InvalidDataException">
        ///   The <paramref name="data"/> does match the <paramref name="signature"/>.
        /// </exception>
        public void Verify(byte[] data, byte[] signature)
        {
            var signer = SignerUtilities.GetSigner(signingAlgorithmName);
            signer.Init(false, publicKey);
            signer.BlockUpdate(data, 0, data.Length);
            if (!signer.VerifySignature(signature))
                throw new InvalidDataException("Data does not match the signature.");
        }

        /// <summary>
        ///   Create a signature for the data.
        /// </summary>
        /// <param name="data">
        ///   The data to sign.
        /// </param>
        /// <returns>
        ///   The signature.
        /// </returns>
        public byte[] Sign(byte[] data)
        {
            var signer = SignerUtilities.GetSigner(signingAlgorithmName);
            signer.Init(true, privateKey);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.GenerateSignature();
        }

        /// <summary>
        ///   Create a public key from the IPFS message.
        /// </summary>
        /// <param name="bytes">
        ///   The IPFS encoded protobuf PublicKey message.
        /// </param>
        /// <returns>
        ///   The public key.
        /// </returns>
        public static Key CreatePublicKeyFromIpfs(byte[] bytes)
        {
            var key = new Key();

            var ms = new MemoryStream(bytes, false);
            var ipfsKey = ProtoBuf.Serializer.Deserialize<PublicKeyMessage>(ms);

            switch (ipfsKey.Type)
            {
                case KeyType.RSA:
                    key.publicKey = PublicKeyFactory.CreateKey(ipfsKey.Data);
                    key.signingAlgorithmName = RsaSigningAlgorithmName;
                    break;
                case KeyType.Secp256k1:
                    key.publicKey = PublicKeyFactory.CreateKey(ipfsKey.Data);
                    key.signingAlgorithmName = EcSigningAlgorithmName;
                    break;
                default:
                    throw new InvalidDataException($"Unknown key type of {ipfsKey.Type}.");
            }
            
            return key;
        }

        /// <summary>
        ///   Create the key from the Bouncy Castle private key.
        /// </summary>
        /// <param name="privateKey">
        ///   The Bouncy Castle private key.
        /// </param>
        public static Key CreatePrivateKey(AsymmetricKeyParameter privateKey)
        {
            var key = new Key();
            key.privateKey = privateKey;

            // Get the public key from the private key.
            if (privateKey is RsaPrivateCrtKeyParameters rsa)
            {
                key.publicKey = new RsaKeyParameters(false, rsa.Modulus, rsa.PublicExponent);
                key.signingAlgorithmName = RsaSigningAlgorithmName;
            }
            else if (privateKey is ECPrivateKeyParameters ec)
            {
                var q = ec.Parameters.G.Multiply(ec.D);
                key.publicKey = new ECPublicKeyParameters(q, ec.Parameters);
                key.signingAlgorithmName = EcSigningAlgorithmName;
            }
            if (key.publicKey == null)
                throw new NotSupportedException($"The key type {privateKey.GetType().Name} is not supported.");

            return key;
        }

        enum KeyType
        {
            RSA = 0,
            Ed25519 = 1,
            Secp256k1 = 2,
            ECDH = 4,
        }

        [ProtoContract]
        class PublicKeyMessage
        {
            [ProtoMember(1, IsRequired = true)]
            public KeyType Type { get; set; }
            [ProtoMember(2, IsRequired = true)]
            public byte[] Data { get; set; }
        }

#if false
        [ProtoContract]
        class PrivateKeyMessage
        {
            [ProtoMember(1, IsRequired = true)]
            public KeyType Type;
            [ProtoMember(2, IsRequired = true)]
            public byte[] Data;
        }
#endif
    }
}
