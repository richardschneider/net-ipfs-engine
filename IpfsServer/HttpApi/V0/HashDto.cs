using Ipfs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipfs.Server.HttpApi.V0
{
    /// <summary>
    ///  A hash to some data.
    /// </summary>
    public class HashDto
    {
        /// <summary>
        ///   Typically a CID.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Hash;

        /// <summary>
        ///   An error message.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Error;
    }
}
