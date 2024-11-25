using MediatR;

namespace Organization.Applications.Shared.BaseController;

[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public class OrganizationBaseController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationBaseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected IMediator Mediator => _mediator;
}