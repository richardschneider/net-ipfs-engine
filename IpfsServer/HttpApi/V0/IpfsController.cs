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
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;

namespace Ipfs.Server.HttpApi.V0
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
        ///   Notifies when the request is cancelled.
        /// </summary>
        /// <value>
        ///   See <see cref="HttpContext.RequestAborted"/>
        /// </value>
        /// <remarks>
        ///   There is no timeout for a request, because of the 
        ///   distributed nature of IPFS.
        /// </remarks>
        protected CancellationToken Cancel
        {
            get
            {
                return HttpContext.RequestAborted;
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

        /// <summary>
        ///   Immediately send the JSON.
        /// </summary>
        /// <param name="o">
        ///   The object to send to the requestor.
        /// </param>
        /// <remarks>
        ///   Immediately sends the Line Delimited JSON (LDJSON) representation
        ///   of <paramref name="o"/> to the  requestor.
        /// </remarks>
        protected async Task StreamJsonAsync(object o)
        {
            if (!Response.HasStarted)
            {
                Response.StatusCode = 200;
                Response.ContentType = "application/json";
            }
            using (var sw = new StringWriter())
            {
                JsonSerializer.Create().Serialize(sw, o);
                sw.Write('\n');
                await Response.WriteAsync(sw.ToString(), Cancel);
                await Response.Body.FlushAsync(Cancel);
            }
        }
    }
}
