using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
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
        public Task<byte[]> CreateProtectedData(
            string keyName, 
            byte[] plainText, 
            CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
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
                .OfType<KeyTransRecipientInformation>()
                .Where(r => r.RecipientID.Issuer.GetValueList(X509Name.OU).Contains("keystore"))
                .Where(r => r.RecipientID.Issuer.GetValueList(X509Name.O).Contains("ipfs"))
                .Select(r =>
                {
                    var keyId = r.RecipientID.Issuer.GetValueList(X509Name.CN)[0] as string;
                    var key = knownKeys.FirstOrDefault(k => k.Id == keyId);
                    return new { recipient = r, key = key };
                })
                .FirstOrDefault(r => r.key != null);
            if (recipient == null)
                throw new KeyNotFoundException("The required decryption key is missing.");

            // Decrypt the contents.
            var decryptionKey = await GetPrivateKeyAsync(recipient.key.Name);
            return recipient.recipient.GetContent(decryptionKey);
        }

    }
}
