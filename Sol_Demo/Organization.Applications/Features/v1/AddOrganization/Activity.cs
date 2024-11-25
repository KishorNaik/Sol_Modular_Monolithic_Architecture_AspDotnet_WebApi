using Models.Shared.Requests;
using Organization.Contracts.Features.AddOrganizations;

namespace Organization.Applications.Features.v1.AddOrganization;

#region Controller

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/organizations")]
[Tags("Organizations")]
public class AddOrganizationController : OrganizationBaseController
{
    public AddOrganizationController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("add")]
    [MapToApiVersion(1)]
    [DisableRateLimiting]
    [AllowAnonymous]
    [ProducesResponseType<DataResponse<AddOrganizationResponseDto>>((int)HttpStatusCode.Created)]
    [ProducesResponseType<DataResponse<AddOrganizationResponseDto>>((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> AddAsync([FromBody] AesRequestDto request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

#endregion Controller