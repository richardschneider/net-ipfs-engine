using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ipfs.Server.Api.V0
{
    /// <summary>
    ///   The standard error response for failing API calls.
    /// </summary>
    public class ApiError
    {
        /// <summary>
        ///   Human readable description of the error.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///   Developer readable description of the error.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] Details { get; set; }

        /// <summary>
        ///   A standard ??? error code.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Code { get; set; }
    }
}
