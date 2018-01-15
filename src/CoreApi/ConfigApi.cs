using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<JObject> GetAsync(CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                var x = await repo.Configs.FindAsync(new string[] { "ipfs" }, cancel);
                return JObject.Parse(x.Value);
            }
        }

        public async Task<JToken> GetAsync(string key, CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                var x = await repo.Configs.FindAsync(new string[] { "ipfs" }, cancel);
                var config = JToken.Parse(x.Value);
                var keys = key.Split('.');
                foreach (var name in keys)
                {
                    config = config[name];
                    if (config == null)
                        throw new KeyNotFoundException($"Configuration setting '{key}' does not exist.");
                }
                return config;
            }
        }

        public Task ReplaceAsync(JObject config)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(string key, string value, CancellationToken cancel = default(CancellationToken))
        {
            return SetAsync(key, JToken.FromObject(value), cancel);
        }

        public async Task SetAsync(string key, JToken value, CancellationToken cancel = default(CancellationToken))
        {
            using (var repo = await ipfs.Repository(cancel))
            {
                var x = await repo.Configs.FindAsync(new string[] { "ipfs" }, cancel);
                var config = JObject.Parse(x.Value);
                var setting = config;

                // Create the object object.
                var keys = key.Split('.');
                foreach (var name in keys.Take(keys.Length - 1))
                {
                    var token = setting[name] as JObject;
                    if (token == null)
                    {
                        token = new JObject();
                        setting[name] = token;
                    }
                    setting = token;
                }
                setting[keys.Last()] = value;
                x.Value = config.ToString();
                await repo.SaveChangesAsync(cancel);
            }
        }
    }
}
