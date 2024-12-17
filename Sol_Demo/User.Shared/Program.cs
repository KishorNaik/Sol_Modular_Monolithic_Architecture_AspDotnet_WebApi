using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using User.Shared.Services.Policy.Requirements;

namespace User.Shared;

public static class Program
{
    public static IServiceCollection AddUserShared(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, AdminOnlyAuthRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, UserOnlyAuthRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, UserAdminOnlyAuthRequirementHandler>(); 
        return services;
    }
}
