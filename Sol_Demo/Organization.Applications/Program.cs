using Frameworks.Aspnetcore.Library.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Models.Shared.Constant;
using Organization.Infrastructures.Context;
using Organization.Infrastructures.Services.AddOrganization;
using User.Shared;

namespace Organization.Applications;

public static class Program
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services, IHostApplicationBuilder hostApplicationBuilder)
    {
        services.AddControllers()
                .AddCustomJson(hostApplicationBuilder.Environment, isPascalCase: true);

        // MediatR Service
        services.AddMediatR((config) =>
        {
            config.RegisterServicesFromAssemblyContaining(typeof(Program));
        });

        // Get Secret Connection String
        string? connectionString = hostApplicationBuilder.Configuration.GetSecretConnectionString(ConstantValue.DbName);

        // Add Database Context
        services.AddDbContext<OrganizationDbContext>((config) =>
        {
            config.UseSqlServer(connectionString);
            config.EnableDetailedErrors(true);
            config.EnableSensitiveDataLogging(true);
        });

        // Auto Register Dependency Injection
        //services.AutoRegisterDependencies();
        services.RegisterServices((config) =>
        {
            config.WithAssemblies(typeof(Program).Assembly, typeof(Organization.Infrastructures.Program).Assembly);
            return config;
        });

        // Add User Shared
        services.AddUserShared();

        return services;
    }
}