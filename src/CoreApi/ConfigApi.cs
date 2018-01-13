using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;

namespace Ipfs.Engine.CoreApi
{
    class ConfigApi : IConfigApi
    {
        IpfsEngine ipfs;

        public ConfigApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<JObject> GetAsync(CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<JToken> GetAsync(string key, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task ReplaceAsync(JObject config)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(string key, string value, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(string key, JToken value, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
