using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;

namespace Ipfs.Engine.CoreApi
{
    class KeyApi : IKeyApi
    {
        IpfsEngine ipfs;

        public KeyApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<IKey> CreateAsync(string name, string keyType, int size, CancellationToken cancel = default(CancellationToken))
        {
            var keyChain = await ipfs.KeyChain(cancel);
            return await keyChain.CreateAsync(name, keyType, size, cancel);
        }

        public async Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default(CancellationToken))
        {
            var keyChain = await ipfs.KeyChain(cancel);
            return await keyChain.ExportAsync(name, password, cancel);
        }

        public async Task<IKey> ImportAsync(string name, string pem, char[] password = null, CancellationToken cancel = default(CancellationToken))
        {
            var keyChain = await ipfs.KeyChain(cancel);
            return await keyChain.ImportAsync(name, pem, password, cancel);
        }

        public async Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            var keyChain = await ipfs.KeyChain(cancel);
            return await keyChain.ListAsync(cancel);
        }

        public async Task<IKey> RemoveAsync(string name, CancellationToken cancel = default(CancellationToken))
        {
            var keyChain = await ipfs.KeyChain(cancel);
            return await keyChain.RemoveAsync(name, cancel);
        }

        public async Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default(CancellationToken))
        {
            var keyChain = await ipfs.KeyChain(cancel);
            return await keyChain.RenameAsync(oldName, newName, cancel);
        }
    }
}
