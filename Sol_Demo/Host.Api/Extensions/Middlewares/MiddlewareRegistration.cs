using Hangfire;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Host.Api.Extensions.Middlewares;

public static class MiddlewareRegistration
{
    public static void MapMiddlewares(this WebApplication app, WebApplicationBuilder builder)
    {
        // Http Logging
        app.UseHttpLogging();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        // Request Decompression
        app.UseRequestDecompression();

        // Use Cors
        app.UseCors("CORS");

        // Use Custom Exception
        app.UseCustomExceptionHandler();

        // Use Response Compression
        app.UseResponseCompression();

        // Use Health Check
        app.MapHealthChecks("/health", new HealthCheckOptions()
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Use API Key
        app.UseApiKeyMiddleware(builder.Configuration);

        // Use Security Middleware
        app.UseSecurityHeadersMiddleware();

        // Use Antiforgery
        app.UseAntiforgery();

        // Use Authorize Exception
        app.UseAuthorizeExceptionMiddleware();

        // Use Jwt
        app.UseJwtToken();

        // Use Rate Limiter
        app.UseRateLimiter();

        // Use Request Timeout
        app.UseRequestTimeouts();

        // Use Hangfire
        app.UseHangfireDashboard();
        app.MapHangfireDashboard("/hangfire");

        app.MapControllers();
    }
}