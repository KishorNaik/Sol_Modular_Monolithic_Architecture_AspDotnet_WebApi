using FluentResults;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Models.Shared.Constant;
using Organization.Applications.Features.v1.AddOrganization;
using Organization.Infrastructures.Services.GetOrganizationByIdentifer;
using System.Threading;
using Utility.Shared.Cache;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Organization.Applications.Shared.Cache;

public class OrganizationSharedCacheServiceParameter
{
    public Guid? Identifier { get; }

    public CancellationToken CancellationToken { get; }

    public OrganizationSharedCacheServiceParameter(Guid? identifier, CancellationToken cancellationToken)
    {
        Identifier = identifier;
        CancellationToken = cancellationToken;
    }
}

public interface IOrganizationSharedCacheService : IServiceHandlerVoidAsync<OrganizationSharedCacheServiceParameter>
{
}

[ScopedService(typeof(IOrganizationSharedCacheService))]
public class OrganizationSharedCacheService : IOrganizationSharedCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IGetOrganizationByIdentifierDbService _getOrganizationByIdentifierDbService;

    public OrganizationSharedCacheService(
        IDistributedCache distributedCache,
        IGetOrganizationByIdentifierDbService getOrganizationByIdentifierDbService
        )
    {
        _distributedCache = distributedCache;
        _getOrganizationByIdentifierDbService = getOrganizationByIdentifierDbService;
    }

    async Task<Result> IServiceHandlerVoidAsync<OrganizationSharedCacheServiceParameter>.HandleAsync(OrganizationSharedCacheServiceParameter @params)
    {
        try
        {
            string cacheName = $"Organization-{@params.Identifier}";

            await SqlCacheHelper.RemoveCacheAsync(_distributedCache, cacheName);

            var organizationResult = await _getOrganizationByIdentifierDbService.HandleAsync(
                new GetOrganizationByIdentifierSqlParameter(@params.Identifier, @params.CancellationToken)
            );

            if (organizationResult.IsFailed)
            {
                return ResultExceptionFactory.Error(organizationResult.Errors[0]);
            }

            await SqlCacheHelper.SetCacheAsync(_distributedCache, cacheName, ConstantValue.CacheTime, organizationResult.Value);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, httpStatusCode: HttpStatusCode.InternalServerError);
        }
    }
}