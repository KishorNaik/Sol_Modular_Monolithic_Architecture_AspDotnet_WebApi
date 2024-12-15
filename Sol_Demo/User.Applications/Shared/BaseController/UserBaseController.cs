using Microsoft.AspNetCore.Mvc;

namespace User.Applications.Shared.BaseController;

[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public class UserBaseController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserBaseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected IMediator Mediator => _mediator;
}
