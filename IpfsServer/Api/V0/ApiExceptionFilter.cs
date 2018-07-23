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
        public override void OnException(ExceptionContext context)
        {
            context.HttpContext.Response.StatusCode = 500;

            // These exceptions indicate the request data is wrong; 400 Bad Request
            if (context.Exception is FormatException)
                context.HttpContext.Response.StatusCode = 400;

            context.Result = new JsonResult(new ApiError { Message = context.Exception.Message });

            base.OnException(context);
        }
    }

}
