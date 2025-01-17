using User.Contracts.Shared.Dtos;

namespace User.Contracts.Features.RefreshToken;


#region Request Dto
public class RefreshTokenRequestDto
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}
#endregion

#region ResponseDto
public class RefreshTokenResponseDto : UseJwtTokenDto
{

}
#endregion 