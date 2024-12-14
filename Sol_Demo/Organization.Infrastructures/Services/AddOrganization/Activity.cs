using FluentResults;
using Microsoft.Data.SqlClient;
using Organization.Infrastructures.Context;
using Organization.Infrastructures.Entities;
using sorovi.DependencyInjection.AutoRegister;
using System.Net;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Organization.Infrastructures.Services.AddOrganization;

public class AddOrganizationSqlParameters
{
    public Torganization? Organization { get; }

    public CancellationToken CancellationToken { get; } = default;

    public AddOrganizationSqlParameters(Torganization? organization, CancellationToken cancellationToken)
    {
        Organization = organization;
        CancellationToken = cancellationToken;
    }
}

public interface IAddOrganizationDbService : IServiceHandlerAsync<AddOrganizationSqlParameters, Torganization>
{
}

[ScopedService(typeof(IAddOrganizationDbService))]
public sealed class AddOrganizationDbService : IAddOrganizationDbService
{
    private readonly OrganizationDbContext _organizationDbContext;

    public AddOrganizationDbService(OrganizationDbContext organizationDbContext)
    {
        _organizationDbContext = organizationDbContext;
    }

    async Task<Result<Torganization>> IServiceHandlerAsync<AddOrganizationSqlParameters, Torganization>.HandleAsync(AddOrganizationSqlParameters @params)
    {
        try
        {
            // Check tuples are empty or not
            if (@params is null)
                return ResultExceptionFactory.Error<Torganization>("Params is null", httpStatusCode: HttpStatusCode.BadRequest);

            // Check organization is null
            if (@params.Organization is null)
                return ResultExceptionFactory.Error<Torganization>("Organization is null", httpStatusCode: HttpStatusCode.BadRequest);

            // Add organization
            await _organizationDbContext.Torganizations.AddAsync(@params.Organization, @params.CancellationToken);
            await _organizationDbContext.SaveChangesAsync(@params.CancellationToken);

            return Result.Ok(@params.Organization);
        }
        catch (Exception ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            return ResultExceptionFactory.Error<Torganization>("Organization already exists", httpStatusCode: HttpStatusCode.Conflict);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error<Torganization>(ex.Message, httpStatusCode: HttpStatusCode.InternalServerError);
        }
    }
}