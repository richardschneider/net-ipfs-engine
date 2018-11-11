using Common.Logging;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
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
        char[] dek;
        FileStore<string, EncryptedKey> store;

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

        FileStore<string, EncryptedKey> Store
        {
            get
            {
                if (store == null)
                {
                    var folder = Path.Combine(ipfs.Options.Repository.Folder, "keys");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    store = new FileStore<string, EncryptedKey>
                    {
                        Folder = folder,
                        NameToKey = (name) => name,
                        KeyToName = (key) => key
                    };
                }
                return store;
            }
        }

        /// <summary>
        ///   The configuration options.
        /// </summary>
        public KeyChainOptions Options { get; set; } = new KeyChainOptions();

        /// <summary>
        ///   Sets the passphrase for the key chain.
        /// </summary>
        /// <param name="passphrase"></param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="UnauthorizedAccessException">
        ///   When the <paramref name="passphrase"/> is wrong.
        /// </exception>
        /// <remarks>
        ///   The <paramref name="passphrase"/> is used to generate a DEK (derived encryption
        ///   key).  The DEK is then used to encrypt the stored keys.
        ///   <para>
        ///   Neither the <paramref name="passphrase"/> nor the DEK are stored.
        ///   </para>
        /// </remarks>
        public async Task SetPassphraseAsync (char[] passphrase, CancellationToken cancel = default(CancellationToken))
        {
            // TODO: Verify DEK options.
            // TODO: get digest based on Options.Hash
            var pdb = new Pkcs5S2ParametersGenerator(new Sha256Digest());
            pdb.Init(
                Encoding.UTF8.GetBytes(passphrase),
                Encoding.UTF8.GetBytes(Options.Dek.Salt),
                Options.Dek.IterationCount);
            var key = (KeyParameter)pdb.GenerateDerivedMacParameters(Options.Dek.KeyLength * 8);
            dek = key.GetKey().ToBase64NoPad().ToCharArray();

            // Verify that that pass phrase is okay, by reading a key.
            var akey = await Store.TryGetAsync("self", cancel);
            if (akey != null)
            {
                try
                {
                    UseEncryptedKey(akey, _ => { });
                }
                catch (Exception e)
                {
                    throw new UnauthorizedAccessException("The pass phrase is wrong.", e);
                }
            }

            log.Debug("Pass phrase is okay");
        }

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
            var key = await Store.TryGetAsync(name, cancel);
            if (key == null)
                return null;
            return new KeyInfo { Id = key.Id, Name = key.Name };
        }

        /// <summary>
        ///   Gets the IPFS encoded public key for the specified key.
        /// </summary>
        /// <param name="name">
        ///   The local name of the key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the IPFS encoded public key.
        /// </returns>
        /// <remarks>
        ///   The IPFS public key is the base-64 encoding of a protobuf encoding containing 
        ///   a type and the DER encoding of the PKCS Subject Public Key Info.
        /// </remarks>
        /// <seealso href="https://tools.ietf.org/html/rfc5280#section-4.1.2.7"/>
        public async Task<string> GetPublicKeyAsync(string name, CancellationToken cancel = default(CancellationToken))
        {
            string result = null;
            var ekey = await Store.TryGetAsync(name, cancel);
            if (ekey != null)
            {
                UseEncryptedKey(ekey, key =>
                {
                    var kp = GetKeyPairFromPrivateKey(key);
                    var spki = SubjectPublicKeyInfoFactory
                        .CreateSubjectPublicKeyInfo(kp.Public)
                        .GetDerEncoded();
                    // Add protobuf cruft.
                    var publicKey = new Proto.PublicKey
                    {
                        Data = spki
                    };
                    if (kp.Public is RsaKeyParameters)
                        publicKey.Type = Proto.KeyType.RSA;
                    else if (kp.Public is ECPublicKeyParameters)
                        publicKey.Type = Proto.KeyType.Secp256k1;
                    else
                        throw new NotSupportedException($"The key type {kp.Public.GetType().Name} is not supported.");

                    using (var ms = new MemoryStream())
                    {
                        ProtoBuf.Serializer.Serialize(ms, publicKey);
                        result = Convert.ToBase64String(ms.ToArray());
                    }
                });
            }
            return result;
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
                case "secp256k1":
                    X9ECParameters ecP = ECNamedCurveTable.GetByName(keyType);
                    if (ecP == null)
                        throw new Exception("unknown curve name: " + keyType);
                    var domain= new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H, ecP.GetSeed());
                    g = GeneratorUtilities.GetKeyPairGenerator("EC");
                    g.Init(new ECKeyGenerationParameters(domain, new SecureRandom()));
                    break;
                default:
                    throw new Exception($"Invalid key type '{keyType}'.");
            }
            var keyPair = g.GenerateKeyPair();
            log.Debug("Created key");

            return await AddPrivateKeyAsync(name, keyPair, cancel);
        }

        /// <inheritdoc />
        public async Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default(CancellationToken))
        {
            string pem = "";
            var key = await Store.GetAsync(name, cancel);
            UseEncryptedKey(key, pkey => 
            {
                using (var sw = new StringWriter())
                {
                    var pkcs8 = new Pkcs8Generator(pkey, Pkcs8Generator.PbeSha1_3DES)
                    {
                        Password = password
                    };
                    var pw = new PemWriter(sw);
                    pw.WriteObject(pkcs8);
                    pw.Writer.Flush();
                    pem = sw.ToString();
                }
            });

            return pem;
        }

        /// <inheritdoc />
        public async Task<IKey> ImportAsync(string name, string pem, char[] password = null, CancellationToken cancel = default(CancellationToken))
        {
            AsymmetricKeyParameter key;
            using (var sr = new StringReader(pem))
            using (var pf = new PasswordFinder { Password = password })
            {
                var reader = new PemReader(sr, pf);
                try
                {
                    key = reader.ReadObject() as AsymmetricKeyParameter;
                }
                catch (Exception e)
                {
                    throw new UnauthorizedAccessException("The password is wrong.", e);
                }
                if (key == null || !key.IsPrivate)
                    throw new InvalidDataException("Not a valid PEM private key");
            }

            return await AddPrivateKeyAsync(name, GetKeyPairFromPrivateKey(key), cancel);
        }

        /// <inheritdoc />
        public Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            var keys = Store
                .Values
                .Select(key => (IKey)new KeyInfo { Id = key.Id, Name = key.Name })
                ;
            return Task.FromResult(keys);
        }

        /// <inheritdoc />
        public async Task<IKey> RemoveAsync(string name, CancellationToken cancel = default(CancellationToken))
        {
            var key = await Store.TryGetAsync(name, cancel);
            if (key == null)
                return null;

            await Store.RemoveAsync(name, cancel);
            return new KeyInfo { Id = key.Id, Name = key.Name };
        }

        /// <inheritdoc />
        public async Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default(CancellationToken))
        {
            var key = await Store.TryGetAsync(oldName, cancel);
            if (key == null)
                return null;
            key.Name = newName;
            await Store.PutAsync(newName, key, cancel);
            await Store.RemoveAsync(oldName,  cancel);

            return new KeyInfo { Id = key.Id, Name = newName };
        }

        /// <summary>
        ///   Gets the Bouncy Castle representation of the private key.
        /// </summary>
        /// <param name="name">
        ///   The local name of key.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the private key as an <see cref="AsymmetricKeyParameter"/>.
        /// </returns>
        public async Task<AsymmetricKeyParameter> GetPrivateKeyAsync(string name, CancellationToken cancel = default(CancellationToken))
        {
            var key = await Store.TryGetAsync(name, cancel);
            if (key == null)
                throw new KeyNotFoundException($"The key 'name' does not exist.");
            AsymmetricKeyParameter kp = null;
            UseEncryptedKey(key, pkey =>
            {
                kp = pkey;
            });
            return kp;
        }

        void UseEncryptedKey(EncryptedKey key, Action<AsymmetricKeyParameter> action)
        {
            using (var sr = new StringReader(key.Pem))
            using (var pf = new PasswordFinder { Password = dek })
            {
                var reader = new PemReader(sr, pf);
                var privateKey = (AsymmetricKeyParameter)reader.ReadObject();
                action(privateKey);
            }
        }

        async Task<IKey> AddPrivateKeyAsync(string name, AsymmetricCipherKeyPair keyPair, CancellationToken cancel)
        {
            // Create the key ID
            var keyId = CreateKeyId(keyPair.Public);

            // Create the PKCS #8 container for the key
            string pem;
            using (var sw = new StringWriter())
            {
                var pkcs8 = new Pkcs8Generator(keyPair.Private, Pkcs8Generator.PbeSha1_3DES)
                {
                    Password = dek
                };
                var pw = new PemWriter(sw);
                pw.WriteObject(pkcs8);
                pw.Writer.Flush();
                pem = sw.ToString();
            }

            // Store the key in the repository.
            var key = new EncryptedKey
            {
                Id = keyId.ToBase58(),
                Name = name,
                Pem = pem
            };
            await Store.PutAsync(name, key);
            log.DebugFormat("Added key '{0}' with ID {1}", name, keyId);

            return new KeyInfo { Id = key.Id, Name = key.Name };
        }

        /// <summary>
        ///   Create a key ID for the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <remarks>
        ///   The key id is the SHA-256 multihash of its public key. The public key is 
        ///   a protobuf encoding containing a type and 
        ///   the DER encoding of the PKCS SubjectPublicKeyInfo.
        /// </remarks>
        MultiHash CreateKeyId (AsymmetricKeyParameter key)
        {
            var spki = SubjectPublicKeyInfoFactory
                .CreateSubjectPublicKeyInfo(key)
                .GetDerEncoded();

            // Add protobuf cruft.
            var publicKey = new Proto.PublicKey
            {
                Data = spki
            };
            if (key is RsaKeyParameters)
                publicKey.Type = Proto.KeyType.RSA;
            else if (key is ECPublicKeyParameters)
                publicKey.Type = Proto.KeyType.Secp256k1;
            else
                throw new NotSupportedException($"The key type {key.GetType().Name} is not supported.");

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, publicKey);
                ms.Position = 0;
                return MultiHash.ComputeHash(ms, "sha2-256");
            }

        }

        AsymmetricCipherKeyPair GetKeyPairFromPrivateKey(AsymmetricKeyParameter privateKey)
        {
            AsymmetricCipherKeyPair keyPair = null;
            if (privateKey is RsaPrivateCrtKeyParameters rsa)
            {
                var pub = new RsaKeyParameters(false, rsa.Modulus, rsa.PublicExponent);
                keyPair = new AsymmetricCipherKeyPair(pub, privateKey);
            }
            if (keyPair == null)
                throw new NotSupportedException($"The key type {privateKey.GetType().Name} is not supported.");

            return keyPair;
        }

        class PasswordFinder : IPasswordFinder, IDisposable
        {
            public char[] Password;

            public void Dispose()
            {
                Password = null;
            }

            public char[] GetPassword()
            {
               return Password;
            }
        }
    }
}
