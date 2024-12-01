using sorovi.DependencyInjection.AutoRegister;
using Utility.Shared.Config;
using Utility.Shared.Response;
using Utility.Shared.Traces;

namespace Host.Api.Extensions.Services
{
    public static class ServiceRegistration
    {
        public static void AddServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddJwtInSwagger();
            });

            // Add Trace Id
            builder.Services.AddTraceIdService();

            // [FromRoute][FromQuery][FromBody] in one Model.
            builder.Services.Configure<ApiBehaviorOptions>((options) => options.SuppressInferBindingSourcesForParameters = true);

            // SeriLog
            builder.AddSeriLogger(dbName: ConstantValue.SeriLogDbName);

            // Request Timeout
            builder.Services.AddRequestTimeouts();

            // Response Compression (Gzip)
            builder.Services.AddGzipResponseCompression(System.IO.Compression.CompressionLevel.Fastest);

            // Request DeCompression
            builder.Services.AddRequestDecompression();

            // Cors
            builder.Services.AddCustomCors(policyName: "CORS");

            // Health Check
            builder.Services.AddCustomHealthChecks(builder.Configuration);

            // Http Logging
            builder.Services.AddHttpLogging(config =>
            {
                config.LoggingFields = HttpLoggingFields.All;
            });

            // Custom Api Version
            builder.Services.AddCustomApiVersion();

            // Anti Forgery
            builder.Services.AddAntiforgery();

            // Sql Distribute Cache
            builder.Services.AddCustomSqlDistributedCache(builder.Configuration, ConstantValue.SqlCacheDbName, "dbo", "DbCache");

            // Background Services(Hangfire)
            builder.Services.AddHangFireBackgroundJob(builder.Configuration, name: ConstantValue.HangFireDbName);
            //builder.Services.AddCustomCoravel();

            // Rate Limit
            builder.Services.AddCustomRateLimit(RateLimitAlgorithmsEnum.SlidingWindow, "sliding",
                new RateLimitOptions(10, TimeSpan.FromSeconds(10), System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst, 30, 2)
            );

            // JWT
            builder.Services.Configure<JwtAppSetting>(builder.Configuration.GetSection("JWT"));
            JwtAppSetting jwtAppSetting = builder.Configuration.GetSection("JWT").Get<JwtAppSetting>()!;
            builder.Services.AddJwtToken(jwtAppSetting);

            // Auto Register Dependency Injection
            //builder.Services.RegisterServices();

            // Add Custom Reading Configuration File (appSettings.json)
            builder.Services.AddConfigService();

            // Add Data Response Builder
            builder.Services.AddDataResponse();
        }
    }
}