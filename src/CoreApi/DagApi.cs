using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Ipfs.Engine.LinkedData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeterO.Cbor;

namespace Ipfs.Engine.CoreApi
{
    class DagApi : IDagApi
    {
        static PODOptions podOptions = new PODOptions
        (
            removeIsPrefix: false,
            useCamelCase: false
        );
        IpfsEngine ipfs;

        public DagApi(IpfsEngine ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<JObject> GetAsync(
            Cid id,
            CancellationToken cancel = default(CancellationToken))
        {
            var block = await ipfs.Block.GetAsync(id, cancel);
            var format = GetDataFormat(id);
            var canonical = format.Deserialise(block.DataBytes);
            using (var ms = new MemoryStream())
            using (var sr = new StreamReader(ms))
            using (var reader = new JsonTextReader(sr))
            {
                canonical.WriteJSONTo(ms);
                ms.Position = 0;
                return (JObject) JObject.ReadFrom(reader);
            }
        }

        public async Task<JToken> GetAsync(
            string path,
            CancellationToken cancel = default(CancellationToken))
        {
            if (path.StartsWith("/ipfs/"))
            {
                path = path.Remove(0, 6);
            }

            var parts = path.Split('/').Where(p => p.Length > 0).ToArray();
            if (parts.Length == 0)
                throw new ArgumentException($"Cannot resolve '{path}'.");

            JToken token = await GetAsync(Cid.Decode(parts[0]), cancel);
            foreach (var child in parts.Skip(1))
            {
                token = ((JObject)token)[child];
                if (token == null)
                    throw new Exception($"Missing component '{child}'.");
            }

            return token;
        }

        public async Task<T> GetAsync<T>(
            Cid id, 
            CancellationToken cancel = default(CancellationToken))
        {
            var block = await ipfs.Block.GetAsync(id, cancel);
            var format = GetDataFormat(id);
            var canonical = format.Deserialise(block.DataBytes);

            // CBOR does not support serialisation to another Type
            // see https://github.com/peteroupc/CBOR/issues/12.
            // So, convert to JSON and use Newtonsoft to deserialise.
            return JObject
                .Parse(canonical.ToJSONString())
                .ToObject<T>();
        }

        public async Task<Cid> PutAsync(
            JObject data,
            string contentType = "dag-cbor",
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = true,
            CancellationToken cancel = default(CancellationToken))
        {
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms))
            using (var writer = new JsonTextWriter(sw))
            {
                data.WriteTo(writer);
                writer.Flush();
                ms.Position = 0;
                var format = GetDataFormat(contentType);
                var block = format.Serialize(CBORObject.ReadJSON(ms));
                return await ipfs.Block.PutAsync(block, contentType, multiHash, encoding, pin, cancel);
            }
        }

        public async Task<Cid> PutAsync(Stream data,
            string contentType = "dag-cbor",
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = true, 
            CancellationToken cancel = default(CancellationToken))
        {
            var format = GetDataFormat(contentType);
            var block = format.Serialize(CBORObject.Read(data));
            return await ipfs.Block.PutAsync(block, contentType, multiHash, encoding, pin, cancel);
        }

        public async Task<Cid> PutAsync(object data,
            string contentType = "dag-cbor",
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = true,
            CancellationToken cancel = default(CancellationToken))
        {
            var format = GetDataFormat(contentType);
            var block = format.Serialize(CBORObject.FromObject(data, podOptions));
            return await ipfs.Block.PutAsync(block, contentType, multiHash, encoding, pin, cancel);
        }

        ILinkedDataFormat GetDataFormat(Cid id)
        {
            if (IpldRegistry.Formats.TryGetValue(id.ContentType, out ILinkedDataFormat format))
                return format;

            throw new Exception($"Unknown IPLD format '{id.ContentType}'.");
        }

        ILinkedDataFormat GetDataFormat(string contentType)
        {
            if (IpldRegistry.Formats.TryGetValue(contentType, out ILinkedDataFormat format))
                return format;

            throw new Exception($"Unknown IPLD format '{contentType}'.");
        }
    }
}
