
using Models.Shared.Enums;
using System.Text.Json.Serialization;
using User.Contracts.Shared.Dtos;

namespace User.Contracts.Features.GetUserByIdentifer;

#region Request DTO
public class GetUserByIdentiferRequestDto
{
    [JsonIgnore]
    public Guid? Identifier { get; set; }
}
#endregion 

#region Response DTO



public class GetUserByIdentiferResponseDto
{
    public Guid? Identifier { get; set; }

    public UserDto? User { get; set; }

    public UserCommunicationDto? Communication { get; set; }

    public UserOrganizationDto? Organization { get; set; }
}
#endregion