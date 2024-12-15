
using Organization.Contracts.Features.GetOrganizationByIdentifier;
using Organization.Contracts.Shared.Events.IsOrgExists;

namespace Organization.Applications.Shared.Events.IsOrgExists;



#region Integration Event Service
public sealed class IsOrganizationExistsIntegrationEventServiceHandler : IRequestHandler<IsOrganizationExistsIntegrationEventService, DataResponse<bool>>
{
    private readonly IMediator _mediator=null;
    private readonly IDataResponseFactory _dataResponseFactory = null;

    public IsOrganizationExistsIntegrationEventServiceHandler(IMediator mediator, IDataResponseFactory dataResponseFactory)
    {
        _mediator = mediator;
        _dataResponseFactory = dataResponseFactory;
    }

    async Task<DataResponse<bool>> IRequestHandler<IsOrganizationExistsIntegrationEventService, DataResponse<bool>>
        .Handle(IsOrganizationExistsIntegrationEventService request, CancellationToken cancellationToken)
    {
       try
        {
            if(request is null)
                return await _dataResponseFactory.ErrorAsync<bool>("Request object is null", (int)HttpStatusCode.BadRequest);

            if(request.Request is null)
                return await _dataResponseFactory.ErrorAsync<bool>("Request object is null", (int)HttpStatusCode.BadRequest);

            var response = await _mediator.Send(
                new GetOrganizationByIdentifierIntegrationEventService(
                        new GetOrganizationByIdentifierRequestDto()
                        {
                            Identifier = request.Request.Identifier
                        }
                    )
                );

            if(response is null)
                return await _dataResponseFactory.ErrorAsync<bool>("Response object is null", (int)HttpStatusCode.InternalServerError);

            if(response.Success==false)
                return await _dataResponseFactory.ErrorAsync<bool>(response?.Message!, Convert.ToInt32( response?.StatusCode));

            return await _dataResponseFactory.SuccessAsync<bool>(Convert.ToInt32(HttpStatusCode.OK), true, response.Message);

        }
        catch(Exception ex)
        {
            return await _dataResponseFactory.ErrorAsync<bool>(ex.Message, (int)HttpStatusCode.InternalServerError);
        }
    }
}
#endregion
