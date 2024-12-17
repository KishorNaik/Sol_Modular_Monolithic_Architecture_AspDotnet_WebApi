using Microsoft.AspNetCore.Authorization;
using Models.Shared.Constant;
using Models.Shared.Enums;
using User.Shared.Services.Policy.Requirements;

namespace User.Shared.Services.Policy.Extensions;

public static class JwtPolicyExtensions
{
    public static Action<AuthorizationOptions>? RegisterPolicy()
    {
        return (option) =>
        {
            option.AddPolicy(ConstantValue.AdminOnlyPolicy, (policy) => policy.Requirements.Add(new AdminOnlyAuthRequirement(UserTypeEnum.Admin)));
            option.AddPolicy(ConstantValue.UserOnlyPolicy, (policy) => policy.Requirements.Add(new UserOnlyAuthRequirement(UserTypeEnum.User)));
            option.AddPolicy(ConstantValue.UserAndAdminPolicy, (policy) => policy.Requirements.Add(new UserAdminOnlyAuthRequirement()));
        };
    }
}
