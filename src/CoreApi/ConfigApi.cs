using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ipfs.Engine.CoreApi
{
    class ConfigApi : IConfigApi
    {
        static JObject defaultConfiguration = JObject.Parse(@"{
  ""Addresses"": {
    ""API"": ""/ip4/127.0.0.1/tcp/5001"",
    ""Gateway"": ""/ip4/127.0.0.1/tcp/8080"",
    ""Swarm"": [
      ""/ip4/0.0.0.0/tcp/4001"",
      ""/ip6/::/tcp/4001""
    ]
  },
}");
 
        IpfsEngine ipfs;
        JObject configuration;

        public ConfigApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<JObject> GetAsync(CancellationToken cancel = default(CancellationToken))
        {
            // If first time, load the confiuration into memory.
            if (configuration == null)
            {
                var path = Path.Combine(ipfs.Options.Repository.ExistingFolder(), "config");
                if (File.Exists(path))
                {
                    using (var reader = File.OpenText(path))
                    using (var jtr = new JsonTextReader(reader))
                    {
                        configuration = await JObject.LoadAsync(jtr);
                    }
                }
                else
                {
                    await ReplaceAsync(defaultConfiguration);
                }
            }

            return configuration;
        }

        public async Task<JToken> GetAsync(string key, CancellationToken cancel = default(CancellationToken))
        {
            JToken config = await GetAsync(cancel);
            var keys = key.Split('.');
            foreach (var name in keys)
            {
                config = config[name];
                if (config == null)
                    throw new KeyNotFoundException($"Configuration setting '{key}' does not exist.");
            }
            return config;
        }

        public Task ReplaceAsync(JObject config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            configuration = config;
            return SaveAsync();
        }

        public Task SetAsync(string key, string value, CancellationToken cancel = default(CancellationToken))
        {
            return SetAsync(key, JToken.FromObject(value), cancel);
        }

        public async Task SetAsync(string key, JToken value, CancellationToken cancel = default(CancellationToken))
        {
            var config = await GetAsync(cancel);

            // If needed, create the setting owner keys.
            var keys = key.Split('.');
            foreach (var name in keys.Take(keys.Length - 1))
            {
                var token = config[name] as JObject;
                if (token == null)
                {
                    token = new JObject();
                    config[name] = token;
                }
                config = token;
            }

            config[keys.Last()] = value;
            await SaveAsync();
        }

        async Task SaveAsync()
        {
            var path = Path.Combine(ipfs.Options.Repository.Folder, "config");
            using (var fs = File.OpenWrite(path))
            using (var writer = new StreamWriter(fs))
            using (var jtw = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
            {
                await configuration.WriteToAsync(jtw);
            }
        }
    }
}
