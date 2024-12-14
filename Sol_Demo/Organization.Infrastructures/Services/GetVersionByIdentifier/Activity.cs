using FluentResults;
using Microsoft.EntityFrameworkCore;
using Organization.Infrastructures.Context;
using sorovi.DependencyInjection.AutoRegister;
using System.Text;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Organization.Infrastructures.Services.GetVersionByIdentifier;

public class GetOrganizationVersionByIdentiferSqlParameter
{
    public Guid? Identifier { get; }

    public CancellationToken CancellationToken { get; }

    public GetOrganizationVersionByIdentiferSqlParameter(Guid? identifier, CancellationToken cancellationToken)
    {
        Identifier = identifier;
        CancellationToken = cancellationToken;
    }
}

public interface IGetOrganizationVersionByIdentiferDbService : IServiceHandlerAsync<GetOrganizationVersionByIdentiferSqlParameter, byte[]>
{
}

[ScopedService(typeof(IGetOrganizationVersionByIdentiferDbService))]
public sealed class GetOrganizationVersionByIdentiferDbService : IGetOrganizationVersionByIdentiferDbService
{
    private readonly OrganizationDbContext _organizationDbContext;

    public GetOrganizationVersionByIdentiferDbService(OrganizationDbContext organizationDbContext)
    {
        _organizationDbContext = organizationDbContext;
    }

    async Task<Result<byte[]>> IServiceHandlerAsync<GetOrganizationVersionByIdentiferSqlParameter, byte[]>.HandleAsync(GetOrganizationVersionByIdentiferSqlParameter @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<byte[]>($"{nameof(GetOrganizationVersionByIdentiferSqlParameter)}", System.Net.HttpStatusCode.BadRequest);

            if (@params.Identifier is null)
                return ResultExceptionFactory.Error<byte[]>($"{nameof(@params.Identifier)}", System.Net.HttpStatusCode.BadRequest);

            var versionResult = (await _organizationDbContext
                .Torganizations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Identifier == @params.Identifier, @params.CancellationToken))?.Version;

            if (versionResult is null)
                return ResultExceptionFactory.Error<byte[]>($"{nameof(@params.Identifier)}", System.Net.HttpStatusCode.NotFound);

            //string versionStr = Encoding.UTF8.GetString(versionResult);

            return Result.Ok(versionResult);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<byte[]>(ex.Message, System.Net.HttpStatusCode.InternalServerError);
        }
    }
}