using Frameworks.Aspnetcore.Library.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Models.Shared.Constant;
using Users.Infrastructures.Contexts;

namespace User.Applications;

public static class Program
{
    public static IServiceCollection AddUserModule(this IServiceCollection services, IHostApplicationBuilder hostApplicationBuilder)
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
        services.AddDbContext<UsersDbContext>((config) =>
        {
            config.UseSqlServer(connectionString);
            config.EnableDetailedErrors(true);
            config.EnableSensitiveDataLogging(true);
        });

        // Auto Register Dependency Injection
        //services.AutoRegisterDependencies();
        services.RegisterServices((config) =>
        {
            config.WithAssemblies(
                typeof(Program).Assembly, 
                typeof(Users.Infrastructures.Program).Assembly,
                typeof(User.Shared.Program).Assembly
                );
            return config;
        });

        return services;
    }
}
