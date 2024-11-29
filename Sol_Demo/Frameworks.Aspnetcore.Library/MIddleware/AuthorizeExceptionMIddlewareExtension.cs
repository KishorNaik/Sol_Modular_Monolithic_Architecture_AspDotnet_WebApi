using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Models.Shared.Responses;
using System.Text.Json;
using Utility.Shared.Traces;

namespace Frameworks.Aspnetcore.Library.MIddleware;

public class AuthorizeExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITraceIdService _traceIdService;

    public AuthorizeExceptionMiddleware(RequestDelegate next, ITraceIdService traceIdService)
    {
        _next = next;
        _traceIdService = traceIdService;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        await _next(httpContext);

        if (httpContext.Response.StatusCode == 401)
        {
            await HandleExceptionAsync(httpContext, "UnAuthorize");
        }
        else if (httpContext.Response.StatusCode == 403)
        {
            await HandleExceptionAsync(httpContext, "Forbidden");
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, string message)
    {
        context.Response.ContentType = "application/json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        };

        string traceId = await _traceIdService.GetOrGenerateTraceId(context);

        var errorHandler = new ErrorHandlerModel(false, context.Response.StatusCode, message, traceId);
        await context.Response.WriteAsJsonAsync(errorHandler, options);
    }
}

public static class AuthorizeExceptionMiddlewareExtension
{
    public static IApplicationBuilder UseAuthorizeExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizeExceptionMiddleware>();
    }
}