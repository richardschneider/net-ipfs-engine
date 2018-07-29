using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Ipfs.Server.Api.V0
{
    /// <summary>
    ///   A link to a CID.
    /// </summary>
    public class LinkedDataDto
    {
        /// <summary>
        ///   The CID.
        /// </summary>
        [JsonProperty(PropertyName = "/")]
        public string Link;
    }

    /// <summary>
    ///   A CID as linked data.
    /// </summary>
    public class LinkedDataCidDto
    {
        /// <summary>
        ///   A link to the CID.
        /// </summary>
        public LinkedDataDto Cid;
    }

    /// <summary>
    ///   Manages the IPLD (linked data) Directed Acrylic Graph.
    /// </summary>
    public class DagController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public DagController(ICoreApi ipfs) : base(ipfs) { }

        /// <summary>
        ///  Resolve a reference (NYI).
        /// </summary>
        [HttpGet, HttpPost, Route("dag/resolve")] // TODO
        public Task Resolve(string arg)
        {
            throw new NotImplementedException("Resolving a dag reference is not implemented.");
        }

        /// <summary>
        ///   Gets the content of some linked data.
        /// </summary>
        /// <param name="arg">
        ///   A path, such as "cid", "/ipfs/cid/" or "cid/a".
        /// </param>
        [HttpGet, HttpPost, Route("dag/get")]
        public async Task<JToken> Get(string arg)
        {
            return await IpfsCore.Dag.GetAsync(arg, Timeout.Token);
        }

        /// <summary>
        ///   Add some linked data.
        /// </summary>
        /// <param name="file">
        ///   multipart/form-data.
        /// </param>
        /// <param name="cidBase">
        ///   The base encoding algorithm.
        /// </param>
        /// <param name="format">
        ///   The content type.
        /// </param>
        /// <param name="hash">
        ///   The hashing algorithm.
        /// </param>
        /// <param name="pin">
        ///   Pin the linked data.
        /// </param>
        [HttpPost("dag/put")]
        public async Task<LinkedDataCidDto> Put(
            IFormFile file,
            string format = "dag-cbor",
            string hash = MultiHash.DefaultAlgorithmName,
            bool pin = true,
            [ModelBinder(Name = "cid-base")] string cidBase = MultiBase.DefaultAlgorithmName)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            using (var stream = file.OpenReadStream())
            using (var sr = new StreamReader(stream))
            using (var tr = new JsonTextReader(sr))
            {
                var serializer = new JsonSerializer();
                JObject json = (JObject)serializer.Deserialize(tr);
                
                var cid = await IpfsCore.Dag.PutAsync(
                    json,
                    contentType: format,
                    multiHash: hash,
                    encoding: cidBase,
                    pin: false,
                    cancel: Timeout.Token);
                return new LinkedDataCidDto { Cid = new LinkedDataDto { Link = cid } };
            }
        }

    }
}
