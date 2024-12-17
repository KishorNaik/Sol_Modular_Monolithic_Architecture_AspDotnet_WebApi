using Models.Shared.Enums;

namespace User.Contracts.Shared.Services.UserClaimsProviders;

public interface IUserClaimsProvider
{
    Guid GetUserIdentifier();

    UserTypeEnum GetUserType();
}
