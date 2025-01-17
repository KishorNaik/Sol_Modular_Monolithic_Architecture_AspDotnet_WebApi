
using User.Contracts.Shared.Dtos;

namespace User.Contracts.Features.Login;

#region Request Dto
public class UserLoginRequestDto
{
    public string? EmailId { get; set; }

    public string? Password { get; set; }
}
#endregion

#region Response Dto


public class UserLoginResponseDto
{
    public Guid? Identifier { get; set; }

    public UserDto? User { get; set; }

    public UserCommunicationDto? Communication { get; set; }

    public UseJwtTokenDto? JwtToken { get; set; }

    public UserCredentialDto? Secrets { get; set; }
}
#endregion 
