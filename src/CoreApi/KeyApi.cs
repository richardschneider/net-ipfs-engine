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

        public Task<IKey> CreateAsync(string name, string keyType, int size, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<string> Export(string name, SecureString password, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IKey> Import(string name, string pem, SecureString password = null, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IKey> RemoveAsync(string name, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
