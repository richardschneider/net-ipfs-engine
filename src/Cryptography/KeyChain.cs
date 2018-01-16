using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

            var keyInfo = new KeyInfo
            {
                Name = name,
                Id = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4"
            };
            var key = new EncryptedKey
            {
                Name = name,
                Pem = "pem"
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
    }
}
