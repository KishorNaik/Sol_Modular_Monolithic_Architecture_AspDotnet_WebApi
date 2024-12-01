using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Models.Shared.Constant;

namespace Frameworks.Aspnetcore.Library.Extensions;

public static class AddCustomHealthChecksServiceExtension
{
    public static void AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // Main Db ConnectionString
        string mainDb = configuration.GetSecretConnectionString(dbSection: ConstantValue.DbName);
        string sqlCache = configuration.GetSecretConnectionString(dbSection: ConstantValue.SqlCacheDbName);
        string seriLogs = configuration.GetSecretConnectionString(dbSection: ConstantValue.SeriLogDbName);
        string hangFire = configuration.GetSecretConnectionString(dbSection: ConstantValue.HangFireDbName);

        services.AddHealthChecks()
                .AddSqlServer(mainDb, name: "MainDB")
                .AddSqlServer(sqlCache, name: "SQLCache")
                .AddSqlServer(seriLogs, name: "SeriLogs")
                .AddSqlServer(hangFire, name: "HangFire");
    }
}