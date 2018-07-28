using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ipfs.Server.Api.V0
{
    /// <summary>
    ///   Handles exceptions thrown by a controller.
    /// </summary>
    /// <remarks>
    ///   Returns a <see cref="ApiError"/> to the caller.
    /// </remarks>
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {
        /// <inheritdoc />
        public override void OnException(ExceptionContext context)
        {
            int statusCode = 500; // Internal Server Error
            string message = context.Exception.Message;
            string[] details = null;

            // Map special exceptions to a status code.
            if (context.Exception is FormatException)
                statusCode = 400; // Bad Request
            else if (context.Exception is KeyNotFoundException)
                statusCode = 400; // Bad Request
            else if (context.Exception is TaskCanceledException)
            {
                statusCode = 504; // Gateway Timeout
                message = "The request took too long to process or was cancelled.";
            }
            else if (context.Exception is NotImplementedException)
            {
                statusCode = 501; // Not Implemented
            }
            else if (context.Exception is System.Reflection.TargetInvocationException)
            {
                message = context.Exception.InnerException.Message;
            }

            // Internal Server Error or Not Implemented get a stack dump.
            if (statusCode == 500 || statusCode == 501)
            {
                details = context.Exception.StackTrace.Split(Environment.NewLine);
            }

            context.HttpContext.Response.StatusCode = statusCode;
            context.Result = new JsonResult(new ApiError { Message = message, Details = details });

            // Remove any caching headers
            context.HttpContext.Response.Headers.Remove("cache-control");
            context.HttpContext.Response.Headers.Remove("etag");
            context.HttpContext.Response.Headers.Remove("last-modified");

            base.OnException(context);
        }
    }

}
