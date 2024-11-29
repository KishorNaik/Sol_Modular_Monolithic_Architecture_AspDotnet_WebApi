using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Utility.Shared.Traces;

namespace Frameworks.Aspnetcore.Library.Extensions;

public class TraceIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITraceIdService _traceIdService;

    public TraceIdEnricher(ITraceIdService traceIdService, IHttpContextAccessor httpContextAccessor)
    {
        _traceIdService = traceIdService;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            var traceId = _traceIdService.GetOrGenerateTraceId(context);
            var traceIdProperty = propertyFactory.CreateProperty("RequestTraceId", traceId);
            logEvent.AddPropertyIfAbsent(traceIdProperty);
        }
    }
}

public static class SeriLoggerExtension
{
    public static void AddSeriLogger(this WebApplicationBuilder webApplicationBuilder, string? dbName)
    {
        // Get Connection String from Secret Conection String Extension.
        var connectionString = webApplicationBuilder.Configuration.GetSecretConnectionString(dbName);

        // Get Service Provider
        var serviceProvider = webApplicationBuilder.Services.BuildServiceProvider();
        var traceIdService = serviceProvider?.GetService<ITraceIdService>();
        var httpContextAccessor = serviceProvider?.GetService<IHttpContextAccessor>();

        // Add Logger Setting
        var logger = new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Error)
                        .MinimumLevel.Override("Serilog", LogEventLevel.Error)
                        .Enrich.FromLogContext()
                        .Enrich.WithClientIp()
                        .Enrich.With(new TraceIdEnricher(traceIdService!, httpContextAccessor!))
                        .WriteTo.Async(option =>
                        {
                            option.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} TraceId:{RequestTraceId} Env:{EnvironmentName} {NewLine} {Exception}");
                            //option.MSSqlServer(connectionString: connectionString!, tableName: "Logs", autoCreateSqlTable: true);
                            option.MSSqlServer(
                                    connectionString: connectionString,
                                    sinkOptions: new MSSqlServerSinkOptions()
                                    {
                                        TableName = "Logs",
                                        AutoCreateSqlTable = true
                                    });
                        })
                        .CreateLogger();

        Log.Logger = logger;
        webApplicationBuilder.Logging.AddSerilog(logger);

        webApplicationBuilder.Host.UseSerilog();
    }
}