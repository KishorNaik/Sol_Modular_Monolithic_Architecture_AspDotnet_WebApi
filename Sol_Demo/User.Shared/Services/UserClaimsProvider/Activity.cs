using Microsoft.AspNetCore.Http;
using Models.Shared.Enums;
using sorovi.DependencyInjection.AutoRegister;
using System.Security.Claims;
using User.Contracts.Shared.Services.UserClaimsProviders;

namespace User.Shared.Services.UserClaimsProvider;

[ScopedService(typeof(IUserClaimsProvider))]
public class UserClaimsProvider : IUserClaimsProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserClaimsProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    Guid IUserClaimsProvider.GetUserIdentifier()
    {
        return Guid.Parse(_httpContextAccessor!.HttpContext!.User.Claims
                        .First(i => i.Type == ClaimTypes.NameIdentifier).Value);
    }

    UserTypeEnum IUserClaimsProvider.GetUserType()
    {
        var userRole = _httpContextAccessor!.HttpContext!.User.Claims.First(i => i.Type == ClaimTypes.Role).Value;

        UserTypeEnum userRoleEnum;
        Enum.TryParse(userRole, out userRoleEnum);
        return userRoleEnum;

    }
}
