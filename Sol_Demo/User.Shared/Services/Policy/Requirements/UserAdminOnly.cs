using Microsoft.AspNetCore.Authorization;
using Models.Shared.Enums;
using System.Security.Claims;

namespace User.Shared.Services.Policy.Requirements;

public class UserAdminOnlyAuthRequirement : IAuthorizationRequirement 
{ 

}

public class UserAdminOnlyAuthRequirementHandler : AuthorizationHandler<UserAdminOnlyAuthRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserAdminOnlyAuthRequirement requirement)
    {
        if (!context.User.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return Task.CompletedTask;
        }

        string roleName = Convert.ToString(context.User.FindFirst(c => c.Type == ClaimTypes.Role).Value);

        if (roleName.ToLower() == UserTypeEnum.Admin!.ToString()!.ToLower()!)
        {
            context.Succeed(requirement);
        }
        else if (roleName.ToLower() == UserTypeEnum.User!.ToString()!.ToLower()!)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
