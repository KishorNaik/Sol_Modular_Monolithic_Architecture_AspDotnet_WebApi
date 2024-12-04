using FluentResults;
using Microsoft.EntityFrameworkCore;
using Organization.Infrastructures.Context;
using Organization.Infrastructures.Entities;
using sorovi.DependencyInjection.AutoRegister;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Organization.Infrastructures.Services.GetOrganizationByIdentifer;

public class GetOrganizationByIdentifierSqlParameter
{
    public Guid? Identifier { get; }

    public CancellationToken CancellationToken { get; }

    public GetOrganizationByIdentifierSqlParameter(Guid? identifier, CancellationToken cancellationToken)
    {
        Identifier = identifier;
        CancellationToken = cancellationToken;
    }
}

public interface IGetOrganizationByIdentifierDbService : IServiceHandlerAsync<GetOrganizationByIdentifierSqlParameter, Torganization>
{
}

[ScopedService(typeof(IGetOrganizationByIdentifierDbService))]
public class GetOrganizationByIdentifierDbService : IGetOrganizationByIdentifierDbService
{
    private readonly OrganizationDbContext _organizationDbContext;

    public GetOrganizationByIdentifierDbService(OrganizationDbContext organizationDbContext)
    {
        _organizationDbContext = organizationDbContext;
    }

    async Task<Result<Torganization>> IServiceHandlerAsync<GetOrganizationByIdentifierSqlParameter, Torganization>.HandleAsync(GetOrganizationByIdentifierSqlParameter @params)
    {
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error<Torganization>($"{nameof(GetOrganizationByIdentifierSqlParameter)} object is null", System.Net.HttpStatusCode.BadRequest);

            if (@params.Identifier is null)
                return ResultExceptionFactory.Error<Torganization>($"{nameof(GetOrganizationByIdentifierSqlParameter.Identifier)} is null", System.Net.HttpStatusCode.BadRequest);

            var organization = await _organizationDbContext
                .Torganizations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Identifier == @params.Identifier, @params.CancellationToken);

            if (organization is null)
                return ResultExceptionFactory.Error<Torganization>("Organization not found", System.Net.HttpStatusCode.NotFound);

            return Result.Ok(organization);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<Torganization>(ex.Message, httpStatusCode: System.Net.HttpStatusCode.InternalServerError);
        }
    }
}