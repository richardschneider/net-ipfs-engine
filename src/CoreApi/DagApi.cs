using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;

namespace Ipfs.Engine.CoreApi
{
    class DagApi : IDagApi
    {
        IpfsEngine ipfs;

        public DagApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<JObject> GetAsync(
            Cid id,
            CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<JToken> GetAsync(
            string path,
            CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<T> GetAsync<T>(
            Cid id, 
            CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Cid> PutAsync(
            JObject data,
            string contentType = "cbor",
            string multiHash = MultiHash.DefaultAlgorithmName,
            bool pin = true,
            CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Cid> PutAsync(Stream data,
            string contentType = "cbor",
            string multiHash = MultiHash.DefaultAlgorithmName, 
            bool pin = true, 
            CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Cid> PutAsync(object data,
            string contentType = "cbor",
            string multiHash = MultiHash.DefaultAlgorithmName,
            bool pin = true,
            CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
