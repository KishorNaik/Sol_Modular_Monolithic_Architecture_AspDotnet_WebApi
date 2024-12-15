using MediatR;
using Models.Shared.Responses;
using System.Text.Json.Serialization;

namespace Organization.Contracts.Shared.Events.IsOrgExists;

#region Request DTO

public class IsOrganizationExistsRequestDto
{
    [JsonIgnore]
    public Guid? Identifier { get; set; }
}

#endregion Request DTO 


#region Integration Event Service

public class IsOrganizationExistsIntegrationEventService : IRequest<DataResponse<bool>>
{
    public IsOrganizationExistsRequestDto Request { get; }

    public IsOrganizationExistsIntegrationEventService(IsOrganizationExistsRequestDto request)
    {
        Request = request;
    }
}

#endregion Integration Event Service