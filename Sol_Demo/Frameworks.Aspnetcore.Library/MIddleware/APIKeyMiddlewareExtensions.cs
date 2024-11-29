using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Models.Shared.Responses;
using Utility.Shared.Traces;

namespace Frameworks.Aspnetcore.Library.MIddleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string APIKEYNAME = "x-api-key";
    private readonly string _apiKey;
    private readonly ITraceIdService _traceIdService;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ITraceIdService traceIdService)
    {
        _next = next;
        _apiKey = configuration["ApiKey"];
        _traceIdService = traceIdService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string traceId = await _traceIdService.GetOrGenerateTraceId(context);

        if (!context.Request.Headers.Keys.Contains(APIKEYNAME))
        {
            context.Response.StatusCode = 400; //Bad Request
            var errorHandler = new ErrorHandlerModel(false, 400, "API Key is missing.", traceId);
            await context.Response.WriteAsJsonAsync(errorHandler);
            return;
        }
        else
        {
            if (context.Request.Headers[APIKEYNAME] != _apiKey) // Here you can check the database or any other service for the API Key
            {
                context.Response.StatusCode = 401; //UnAuthorized
                var errorHandler = new ErrorHandlerModel(false, 401, "UnAuthorized.", traceId);
                await context.Response.WriteAsJsonAsync(errorHandler);
                return;
            }
        }

        await _next.Invoke(context);
    }
}

public static class ApiKeyMiddlewareExtension
{
    public static IApplicationBuilder UseApiKeyMiddleware(this IApplicationBuilder builder, IConfiguration configuration)
    {
        var traceIdService = builder.ApplicationServices.GetRequiredService<ITraceIdService>();

        return builder.UseMiddleware<ApiKeyMiddleware>(configuration, traceIdService);
    }
}