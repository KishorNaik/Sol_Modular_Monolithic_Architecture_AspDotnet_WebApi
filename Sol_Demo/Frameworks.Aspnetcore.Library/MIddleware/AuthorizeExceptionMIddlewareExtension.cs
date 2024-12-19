using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models.Shared.Responses;
using System.Text.Json;
using Utility.Shared.Traces;

namespace Frameworks.Aspnetcore.Library.MIddleware;

public class AuthorizeExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITraceIdService _traceIdService;
    private readonly ILogger<AuthorizeExceptionMiddleware> _logger;

    public AuthorizeExceptionMiddleware(RequestDelegate next, ITraceIdService traceIdService, ILogger<AuthorizeExceptionMiddleware> logger)
    {
        _next = next;
        _traceIdService = traceIdService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        await _next(httpContext);

        if (!httpContext.Response.HasStarted)
        {

            if (httpContext.Response.StatusCode == 401)
            {
                await HandleExceptionAsync(httpContext, "UnAuthorize");
            }
            else if (httpContext.Response.StatusCode == 403)
            {
                await HandleExceptionAsync(httpContext, "Forbidden");
            }
        }
        else
        {
            _logger.LogWarning("Response has already started, cannot modify headers or body.");
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, string message)
    {
        try
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
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
            _logger.LogInformation($"Explicitly handled exception in {nameof(AuthorizeExceptionMiddleware)}");
        }
    }
}

public static class AuthorizeExceptionMiddlewareExtension
{
    public static IApplicationBuilder UseAuthorizeExceptionMiddleware(this IApplicationBuilder builder)
    {
        var traceIdService = builder.ApplicationServices.GetRequiredService<ITraceIdService>();

        return builder.UseMiddleware<AuthorizeExceptionMiddleware>(traceIdService);
    }
}