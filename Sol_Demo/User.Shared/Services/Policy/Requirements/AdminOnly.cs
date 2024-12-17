using Microsoft.AspNetCore.Authorization;
using Models.Shared.Enums;
using System.Security.Claims;

namespace User.Shared.Services.Policy.Requirements;

public class AdminOnlyAuthRequirement: IAuthorizationRequirement
{

    public UserTypeEnum UserType { get; }

    public AdminOnlyAuthRequirement(UserTypeEnum userTypeEnum) 
    { 
        UserType = userTypeEnum;
    }
}

public class AdminOnlyAuthRequirementHandler : AuthorizationHandler<UserOnlyAuthRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserOnlyAuthRequirement requirement)
    {
        if (!context.User.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return Task.CompletedTask;
        }

        string roleName = Convert.ToString(context!.User!.FindFirst(c => c.Type == ClaimTypes.Role)!.Value);

        if (roleName.ToLower() == UserTypeEnum.Admin!.ToString()!.ToLower()!)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}