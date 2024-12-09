using MediatR;
using Models.Shared.Responses;
using System.Text.Json.Serialization;

namespace Organization.Contracts.Features.GetOrganizationByIdentifier;

#region Request DTO

public class GetOrganizationByIdentifierRequestDto
{
    [JsonIgnore]
    public Guid? Identifier { get; set; }
}

#endregion Request DTO

#region Response DTO

public class GetOrganizationByIdentifierResponseDto
{
    public Guid? Identifier { get; set; }
    public string? Name { get; set; }
}

#endregion Response DTO

#region Integration Event Service

public class GetOrganizationByIdentifierIntegrationEventService : IRequest<DataResponse<GetOrganizationByIdentifierResponseDto>>
{
    public GetOrganizationByIdentifierRequestDto Request { get; }

    public GetOrganizationByIdentifierIntegrationEventService(GetOrganizationByIdentifierRequestDto request)
    {
        Request = request;
    }
}

#endregion Integration Event Service