using FluentResults;
using MediatR;

namespace User.Contracts.Shared.Events.HmacSignature;

public class GetHmacSecretKeyIntegrationEventService : IRequest<Result<string>>
{
    public string? ClientId { get; }

    public GetHmacSecretKeyIntegrationEventService(string? clientId)
    {
        ClientId = clientId;
    }
}
