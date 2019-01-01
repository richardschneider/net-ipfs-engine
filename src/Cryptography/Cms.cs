using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine.Cryptography
{
    public partial class KeyChain
    {
        /// <summary>
        ///   Encrypt data as CMS protected data.
        /// </summary>
        /// <param name="keyName">
        ///   The key name to protect the <paramref name="plainText"/> with.
        /// </param>
        /// <param name="plainText">
        ///   The data to protect.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the cipher text of the <paramref name="plainText"/>.
        /// </returns>
        /// <remarks>
        ///   Cryptographic Message Syntax (CMS), aka PKCS #7 and 
        ///   <see href="https://tools.ietf.org/html/rfc5652">RFC 5652</see>,
        ///   describes an encapsulation syntax for data protection. It
        ///   is used to digitally sign, digest, authenticate, and/or encrypt
        ///   arbitrary message content.
        /// </remarks>
        public async Task<byte[]> CreateProtectedData(
            string keyName, 
            byte[] plainText, 
            CancellationToken cancel = default(CancellationToken))
        {
            // Identify the recipient by the Subject Key ID.

            // TODO: Need a method to just the get BC public key
            // Get the BC key pair for the named key.
            var ekey = await Store.TryGetAsync(keyName, cancel);
            if (ekey == null)
                throw new KeyNotFoundException($"The key '{keyName}' does not exist.");
            AsymmetricCipherKeyPair kp = null;
            UseEncryptedKey(ekey, key =>
            {
                kp = this.GetKeyPairFromPrivateKey(key);
            });

            // Add recipient type based on key type.
            var edGen = new CmsEnvelopedDataGenerator();
            if (kp.Private is RsaPrivateCrtKeyParameters)
            {
                edGen.AddKeyTransRecipient(kp.Public, Base58.Decode(ekey.Id));
            }
            else if (kp.Private is ECPrivateKeyParameters)
            {
                var cert = await CreateBCCertificateAsync(keyName, cancel);
                edGen.AddKeyAgreementRecipient(
                    agreementAlgorithm: CmsEnvelopedDataGenerator.ECDHSha1Kdf,
                    senderPrivateKey: kp.Private,
                    senderPublicKey: kp.Public,
                    recipientCert: cert,
                    cekWrapAlgorithm: CmsEnvelopedDataGenerator.Aes256Wrap
                    );
            }
            else
            {
                throw new NotSupportedException($"The key type {kp.Private.GetType().Name} is not supported.");
            }

            // Generate the protected data.
            var ed = edGen.Generate(
                new CmsProcessableByteArray(plainText),
                CmsEnvelopedDataGenerator.Aes256Cbc);
            return ed.GetEncoded();
        }

        /// <summary>
        ///   Decrypt CMS protected data.
        /// </summary>
        /// <param name="cipherText">
        ///   The protected CMS data.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the plain text byte array of the protected data.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        ///   When the required private key, to decrypt the data, is not foumd.
        /// </exception>
        /// <remarks>
        ///   Cryptographic Message Syntax (CMS), aka PKCS #7 and 
        ///   <see href="https://tools.ietf.org/html/rfc5652">RFC 5652</see>,
        ///   describes an encapsulation syntax for data protection. It
        ///   is used to digitally sign, digest, authenticate, and/or encrypt
        ///   arbitrary message content.
        /// </remarks>
        public async Task<byte[]> ReadProtectedData(
            byte[] cipherText,
            CancellationToken cancel = default(CancellationToken))
        {
            var cms = new CmsEnvelopedDataParser(cipherText);

            // Find a recipient whose key we hold. We only deal with recipient names
            // issued by ipfs (O=ipfs, OU=keystore).
            var knownKeys = (await ListAsync(cancel)).ToArray();
            var recipient = cms
                .GetRecipientInfos()
                .GetRecipients()
                .OfType<RecipientInformation>()
                .Select(ri =>
                {
                    var kid = GetKeyId(ri);
                    var key = knownKeys.FirstOrDefault(k => k.Id == kid);
                    return new { recipient = ri, key = key };
                })
                .FirstOrDefault(r => r.key != null);
            if (recipient == null)
                throw new KeyNotFoundException("The required decryption key is missing.");

            // Decrypt the contents.
            var decryptionKey = await GetPrivateKeyAsync(recipient.key.Name);
            return recipient.recipient.GetContent(decryptionKey);
        }

        /// <summary>
        ///   Get the key ID for a recipient.
        /// </summary>
        /// <param name="ri">
        ///   A recepient of the message.
        /// </param>
        /// <returns>
        ///   The key ID of the recepient or <b>null</b> if the recepient info
        ///   is not understood or does not contain an IPFS key id.
        /// </returns>
        /// <remarks>
        ///   The key ID is either the Subject Key Identifier (preferred) or the
        ///   issuer's distinguished name with the form "CN=&lt;kid>,OU=keystore,O=ipfs".
        /// </remarks>
        MultiHash GetKeyId(RecipientInformation ri)
        {
            // Any errors are simply ignored.
            try
            {
                // Subject Key Identifier is the key ID.
                if (ri.RecipientID.SubjectKeyIdentifier is byte[] ski)
                    return new MultiHash(ski);

                // Issuer is CN=<kid>,OU=keystore,O=ipfs
                var issuer = ri.RecipientID.Issuer;
                if (issuer != null
                    && issuer.GetValueList(X509Name.OU).Contains("keystore")
                    && issuer.GetValueList(X509Name.O).Contains("ipfs"))
                {
                    var cn = issuer.GetValueList(X509Name.CN)[0] as string;
                    return new MultiHash(cn);
                }
            }
            catch (Exception e)
            {
                log.Warn("Failed reading CMS recipient info.", e);
            }

            return null;
        }

    }
}
