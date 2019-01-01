using Org.BouncyCastle.Asn1.EdEC;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine.Cryptography
{
    public partial class KeyChain
    {
        /// <summary>
        ///   Create a X509 certificate for the specified key.
        /// </summary>
        /// <param name="keyName">
        ///   The key name.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<byte[]> CreateCertificateAsync(
            string keyName, 
            CancellationToken cancel = default(CancellationToken))
        {
            var cert = await CreateBCCertificateAsync(keyName, cancel);
            return cert.GetEncoded();
        }

        /// <summary>
        ///   Create a X509 certificate for the specified key.
        /// </summary>
        /// <param name="keyName">
        ///   The key name.
        /// </param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<X509Certificate> CreateBCCertificateAsync(
            string keyName,
            CancellationToken cancel = default(CancellationToken))
        {
            // Get the BC key pair for the named key.
            var ekey = await Store.TryGetAsync(keyName, cancel);
            if (ekey == null)
                throw new KeyNotFoundException($"The key '{keyName}' does not exist.");
            AsymmetricCipherKeyPair kp = null;
            UseEncryptedKey(ekey, key =>
            {
                kp = this.GetKeyPairFromPrivateKey(key);
            });

            // A signer for the key.
            var ku = new KeyUsage(KeyUsage.DigitalSignature 
                | KeyUsage.DataEncipherment
                | KeyUsage.KeyEncipherment);
            ISignatureFactory signatureFactory = null;
            if (kp.Private is ECPrivateKeyParameters)
            {
                signatureFactory = new Asn1SignatureFactory(
                    X9ObjectIdentifiers.ECDsaWithSha256.ToString(),
                    kp.Private);
            }
            else if (kp.Private is RsaPrivateCrtKeyParameters)
            {
                signatureFactory = new Asn1SignatureFactory(
                    PkcsObjectIdentifiers.Sha256WithRsaEncryption.ToString(),
                    kp.Private);
            }
            else if (kp.Private is Ed25519PrivateKeyParameters)
            {
                signatureFactory = new Asn1SignatureFactory(
                    EdECObjectIdentifiers.id_Ed25519.Id.ToString(),
                    kp.Private);
                ku = new KeyUsage(KeyUsage.DigitalSignature);
            }
            if (signatureFactory == null)
            {
                throw new NotSupportedException($"The key type {kp.Private.GetType().Name} is not supported.");
            }

            // Build the certificate.
            var dn = new X509Name($"CN={ekey.Id}, OU=keystore, O=ipfs");
            var ski = new SubjectKeyIdentifier(Base58.Decode(ekey.Id));
            // Not a certificate authority.
            // TODO: perhaps the "self" key is a CA and all other keys issued by it.
            var bc = new BasicConstraints(false);

            var certGenerator = new X509V3CertificateGenerator();
            certGenerator.SetIssuerDN(dn);
            certGenerator.SetSubjectDN(dn);
            certGenerator.SetSerialNumber(BigInteger.ValueOf(1));
            certGenerator.SetNotAfter(DateTime.UtcNow.AddYears(10));
            certGenerator.SetNotBefore(DateTime.UtcNow);
            certGenerator.SetPublicKey(kp.Public);
            certGenerator.AddExtension(X509Extensions.SubjectKeyIdentifier, false, ski);
            certGenerator.AddExtension(X509Extensions.BasicConstraints, true, bc);
            certGenerator.AddExtension(X509Extensions.KeyUsage, false, ku);

            return certGenerator.Generate(signatureFactory);
        }

    }
}
