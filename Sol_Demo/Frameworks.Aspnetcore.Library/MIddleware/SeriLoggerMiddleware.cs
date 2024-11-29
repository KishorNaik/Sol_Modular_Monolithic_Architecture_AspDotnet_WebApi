using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Utility.Shared.Traces;

namespace Frameworks.Aspnetcore.Library.MIddleware;

public class RequestResponseLogModel
{
    public string? LogId { get; set; }
    public string? ClientIp { get; set; }
    public string? TraceId { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestQuery { get; set; }
    public string? RequestMethod { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
}

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly ITraceIdService _traceIdService;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger, ITraceIdService traceIdService)
    {
        _next = next;
        _logger = logger;
        _traceIdService = traceIdService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Clone the request body
        context.Request.EnableBuffering();
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        // Capture the response body
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        await _next(context);

        // Read the response body
        context.Request.Body.Position = 0;
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        // Log the request and response
        var logModel = new RequestResponseLogModel
        {
            LogId = Guid.NewGuid().ToString(),
            ClientIp = context.Connection.RemoteIpAddress?.ToString(),
            TraceId = await _traceIdService.GetOrGenerateTraceId(context),
            RequestPath = context.Request.Path,
            RequestQuery = context.Request.QueryString.ToString(),
            RequestMethod = context.Request.Method,
            RequestBody = requestBody,
            ResponseBody = responseBody
        };

        _logger.LogInformation("HTTP => LogId:{@LogId} ClientIp:{@ClientIp} TraceId:{@TraceId} RequestPath:{@RequestPath} RequestQuery:{@RequestQuery} RequestMethod:{@RequestMethod} RequestBody:{@RequestBody} ResponseBody:{@ResponseBody}",
            logModel.LogId, logModel.ClientIp, logModel.TraceId, logModel.RequestPath, logModel.RequestQuery, logModel.RequestMethod, logModel.RequestBody, logModel.ResponseBody);

        // Copy the contents of the new memory stream (which contains the response) to the original stream
        await responseBodyStream.CopyToAsync(originalResponseBodyStream);
    }
}

public static class SerilogMiddlewareExtension
{
    public static void UseSeriLogs(this WebApplication app)
    {
        var traceIdService = app.Services.GetRequiredService<ITraceIdService>();

        app.UseSerilogRequestLogging();
        app.UseMiddleware<RequestResponseLoggingMiddleware>(traceIdService);
    }
}