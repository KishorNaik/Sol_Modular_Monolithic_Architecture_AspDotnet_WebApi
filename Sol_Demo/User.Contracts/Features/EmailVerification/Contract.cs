using System.Text.Json.Serialization;

namespace User.Contracts.Features.EmailVerification;

public class UserEmailVerificationRequestDto
{
    [JsonIgnore]
    public Guid? Token { get; set; }
}
