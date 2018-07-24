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
            int statusCode = 500;
            string message = context.Exception.Message;

            // Map special exceptions to a status code.
            if (context.Exception is FormatException)
                statusCode = 400; // Bad Request
            else if (context.Exception is TaskCanceledException)
            {
                statusCode = 504; // Gateway Timeout
                message = "The request took too long to process.";
            }

            context.HttpContext.Response.StatusCode = statusCode;
            context.Result = new JsonResult(new ApiError { Message = message });

            base.OnException(context);
        }
    }

}
