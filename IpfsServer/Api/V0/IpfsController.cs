using Ipfs;
using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Ipfs.Server.Api.V0
{
    /// <summary>
    ///   A base controller for IPFS HTTP API.
    /// </summary>
    /// <remarks>
    ///   Any unhandled exceptions are translated into an <see cref="ApiError"/> by the
    ///   <see cref="ApiExceptionFilter"/>.
    /// </remarks>
    [Route("api/v0")]
    [Produces("application/json")]
    [ApiExceptionFilter]
    public abstract class IpfsController : Controller
    {
        CancellationTokenSource timeout;

        /// <summary>
        ///   Creates a new instance of the controller.
        /// </summary>
        /// <param name="ipfs">
        ///   An implementation of the IPFS Core API.
        /// </param>
        public IpfsController(ICoreApi ipfs)
        {
            IpfsCore = ipfs;
        }

        /// <summary>
        ///   An implementation of the IPFS Core API.
        /// </summary>
        protected ICoreApi IpfsCore { get; }

        /// <summary>
        ///   The default timeout for an API request.
        /// </summary>
        /// <value>
        ///   30 seconds.
        /// </value>
        protected CancellationTokenSource Timeout
        {
            get
            {
                timeout = timeout ?? new CancellationTokenSource(TimeSpan.FromSeconds(30)); 
                return timeout;
            }
        }

        /// <summary>
        ///   Declare that the response is immutable and should be cached forever.
        /// </summary>
        protected void Immutable()
        {
            Response.Headers.Add("cache-control", new StringValues("public, max-age=31536000, immutable"));
        }

        /// <summary>
        ///   Get the strong ETag for a CID.
        /// </summary>
        protected EntityTagHeaderValue ETag(Cid id)
        {
            return new EntityTagHeaderValue(new StringSegment("\"" + id + "\""), isWeak: false);
        }
    }
}
