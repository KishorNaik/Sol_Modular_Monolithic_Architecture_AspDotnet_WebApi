using FluentResults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Models.Shared.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Utility.Shared.Traces;

namespace Frameworks.Aspnetcore.Library.MIddleware
{
    public static class ExceptionMiddlewareExtensions
    {
        public static void UseCustomExceptionHandler(this IApplicationBuilder app)
        {
            _ = app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    //context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";

                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = null
                    };

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        var iTraceIdService = context.RequestServices.GetRequiredService<ITraceIdService>();
                        string traceId = await iTraceIdService.GetOrGenerateTraceId(context);

                        var errorHandler = new ErrorHandlerModel(false, context.Response.StatusCode, contextFeature.Error.Message, traceId);

                        await context.Response.WriteAsJsonAsync(errorHandler, options);
                    }
                });
            });
        }
    }
}