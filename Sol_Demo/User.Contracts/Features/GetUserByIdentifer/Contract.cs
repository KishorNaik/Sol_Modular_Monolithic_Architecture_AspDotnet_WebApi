
using Models.Shared.Enums;
using System.Text.Json.Serialization;

namespace User.Contracts.Features.GetUserByIdentifer;

#region Request DTO
public class GetUserByIdentiferRequestDto
{
    [JsonIgnore]
    public Guid? Identifier { get; set; }
}
#endregion 

#region Response DTO

public class UserDto
{

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public UserTypeEnum UserType { get; set; }

    public StatusEnum Status { get; set; }
}

public class UserCommunicationDto
{
    public string? EmailId { get; set; }

    public string? MobileNumber { get; set; }
}

public class UserOrganizationDto
{
    public Guid? OrgId { get; set; }
}

public class GetUserByIdentiferResponseDto
{
    public Guid? Identifier { get; set; }

    public UserDto? User { get; set; }

    public UserCommunicationDto? Communication { get; set; }

    public UserOrganizationDto? Organization { get; set; }
}
#endregion