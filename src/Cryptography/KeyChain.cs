using Common.Logging;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine.Cryptography
{
    /// <summary>
    ///   A secure key chain.
    /// </summary>
    public class KeyChain : Ipfs.CoreApi.IKeyApi
    {
        static ILog log = LogManager.GetLogger(typeof(KeyChain));

        IpfsEngine ipfs;

        /// <summary>
        ///   Create a new instance of the <see cref="KeyChain"/> class.
        /// </summary>
        /// <param name="ipfs">
        ///   The IPFS Engine associated with the key chain.
        /// </param>
        public KeyChain(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>
        ///   The configuration options.
        /// </summary>
        public KeyChainOptions Options { get; set; } = new KeyChainOptions();

        /// <summary>
        ///   Find a key by its name.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   an <see cref="IKey"/> or <b>null</b> if the the key is not defined.
        /// </returns>
        public async Task<IKey> FindKeyByNameAsync(string name, CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                return await repo.Keys
                    .Where(k => k.Name == name)
                    .FirstOrDefaultAsync(cancel);
            }
        }

        /// <inheritdoc />
        public async Task<IKey> CreateAsync(string name, string keyType, int size, CancellationToken cancel = default(CancellationToken))
        {
            // Apply defaults.
            if (string.IsNullOrWhiteSpace(keyType))
                keyType = Options.DefaultKeyType;
            if (size < 1)
                size = Options.DefaultKeySize;
            keyType = keyType.ToLowerInvariant();

            // Create the key pair.
            log.DebugFormat("Creating {0} key named '{1}'", keyType, name);
            IAsymmetricCipherKeyPairGenerator g;
            switch (keyType)
            {
                case "rsa":
                    g = GeneratorUtilities.GetKeyPairGenerator("RSA");
                    g.Init(new RsaKeyGenerationParameters(
                        BigInteger.ValueOf(0x10001), new SecureRandom(), size, 25));
                    break;
                default:
                    throw new Exception($"Invalid key type '{keyType}'.");
            }
            var keyPair = g.GenerateKeyPair();
            log.Debug("Created key");

            // Create the key ID
            var keyId = CreateKeyId(keyType, keyPair);

            // Create the PKCS #8 container for the key
            string pem;
            using (var sw = new StringWriter())
            {
                var pkcs8 = new Pkcs8Generator(keyPair.Private, Pkcs8Generator.PbeSha1_3DES)
                {
                    // TODO: need the dek
                    Password = "hello".ToCharArray()
                };
                var pw = new PemWriter(sw);
                pw.WriteObject(pkcs8);
                pw.Writer.Flush();
                pem = sw.ToString();
            }

            // Store the key in the repository.
            var keyInfo = new KeyInfo
            {
                Name = name,
                Id = keyId
            };
            var key = new EncryptedKey
            {
                Name = name,
                Pem = pem
            };
            using (var repo = await ipfs.Repository(cancel))
            {
                await repo.AddAsync(keyInfo, cancel);
                await repo.AddAsync(key, cancel);
                await repo.SaveChangesAsync(cancel);
                return keyInfo;
            }
        }

        /// <inheritdoc />
        public Task<string> Export(string name, SecureString password, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<IKey> Import(string name, string pem, SecureString password = null, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                return await repo.Keys.ToArrayAsync(cancel);
            }
        }

        /// <inheritdoc />
        public async Task<IKey> RemoveAsync(string name, CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                var pk = new string[] { name };
                var keyInfo = await repo.Keys.FindAsync(pk, cancel);
                repo.Keys.Remove(keyInfo);
                var key = await repo.EncryptedKeys.FindAsync(pk, cancel);
                repo.EncryptedKeys.Remove(key);
                await repo.SaveChangesAsync(cancel);

                return keyInfo;
            }
        }

        /// <inheritdoc />
        public async Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                var pk = new string[] { oldName };
                var keyInfo = await repo.Keys.FindAsync(pk, cancel);
                var key = await repo.EncryptedKeys.FindAsync(pk, cancel);
                key.Name = newName;
                keyInfo.Name = newName;
                await repo.SaveChangesAsync(cancel);

                return keyInfo;
            }
        }

        /// <summary>
        ///   Create a key ID for the key.
        /// </summary>
        /// <param name="keyType"></param>
        /// <param name="keyPair"></param>
        /// <returns></returns>
        /// <remarks>
        ///   The key id is the SHA-256 multihash of its public key. The public key is 
        ///   a protobuf encoding containing a type and 
        ///   the DER encoding of the PKCS SubjectPublicKeyInfo.
        /// </remarks>
        MultiHash CreateKeyId (string keyType, AsymmetricCipherKeyPair keyPair)
        {
            var spki = SubjectPublicKeyInfoFactory
                .CreateSubjectPublicKeyInfo(keyPair.Public)
                .GetDerEncoded();

            // TODO: add protobuf cruft.
            using (var ms = new MemoryStream())
            {
                ms.Write(spki, 0, spki.Length);

                ms.Position = 0;
                return MultiHash.ComputeHash(ms, "sha2-256");
            }

        }
    }
}
