﻿
using User.Contracts.Shared.Dtos;

namespace User.Contracts.Features.Login;

#region Request Dto
public class UserLoginRequestDto
{
    public string? EmailId { get; }

    public string? Password { get; }
}
#endregion

#region Response Dto
public class UseJwtTokenDto
{
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
}

public class UserLoginResponseDto
{
    public Guid? Identifier { get; set; }

    public UserDto? User { get; set; }

    public UserCommunicationDto? Communication { get; set; }

    public UseJwtTokenDto? JwtToken { get; set; }
}
#endregion 