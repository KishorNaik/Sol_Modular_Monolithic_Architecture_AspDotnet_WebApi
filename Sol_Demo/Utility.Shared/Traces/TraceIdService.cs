using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Utility.Shared.Traces;

public interface ITraceIdService
{
    Task<string> GetOrGenerateTraceId(HttpContext httpContext);
}

public class TraceIdService : ITraceIdService
{
    private const string TraceIdKey = "TraceId";

    public TraceIdService()
    {
    }

    public Task<string> GetOrGenerateTraceId(HttpContext httpContext)
    {
        var context = httpContext;

        if (!context.Items.ContainsKey(TraceIdKey))
        {
            context.Items[TraceIdKey] = Guid.NewGuid().ToString();
        }

        return Task.FromResult<string>((context.Items[TraceIdKey] as string)!);
    }
}

public static class TraceIdServiceExtension
{
    public static IServiceCollection AddTraceIdService(this IServiceCollection services)
    {
        services.AddSingleton<ITraceIdService, TraceIdService>();
        return services;
    }
}