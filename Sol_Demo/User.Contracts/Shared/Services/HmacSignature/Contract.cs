using FluentResults;
using sorovi.DependencyInjection.AutoRegister;
using Utility.Shared.ServiceHandler;

namespace User.Contracts.Shared.Service.HmacSignature;

public class GetHmacSecretKeyServiceParameters
{
    public string? ClientId { get; }

    public CancellationToken CancellationToken { get; }

    public GetHmacSecretKeyServiceParameters(string? clientId, CancellationToken cancellationToken)
    {
        ClientId = clientId;
        CancellationToken = cancellationToken;
    }
}


public interface IGetHmacSecretKeyService : IServiceHandlerAsync<GetHmacSecretKeyServiceParameters,string>
{

}
