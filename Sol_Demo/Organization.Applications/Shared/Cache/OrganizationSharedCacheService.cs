using FluentResults;
using Microsoft.Extensions.Caching.Distributed;
using Models.Shared.Constant;
using Newtonsoft.Json;
using Organization.Infrastructures.Entities;
using Organization.Infrastructures.Services.GetOrganizationByIdentifer;
using Organization.Infrastructures.Services.GetVersionByIdentifier;
using System.Collections;
using System.Text;
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

public class OrganizationSharedCacheServiceResult
{
    public Torganization? Torganization { get; }

    public bool? IsCached { get; }

    public OrganizationSharedCacheServiceResult(Torganization? torganization, bool? isCached)
    {
        Torganization = torganization;
        IsCached = isCached;
    }
}

public interface IOrganizationSharedCacheService : IServiceHandlerAsync<OrganizationSharedCacheServiceParameter, OrganizationSharedCacheServiceResult>
{
}

[ScopedService(typeof(IOrganizationSharedCacheService))]
public class OrganizationSharedCacheService : IOrganizationSharedCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IGetOrganizationByIdentifierDbService _getOrganizationByIdentifierDbService;
    private readonly IGetOrganizationVersionByIdentiferDbService _getOrganizationVersionByIdentiferDbService = null;

    public OrganizationSharedCacheService(
        IDistributedCache distributedCache,
        IGetOrganizationByIdentifierDbService getOrganizationByIdentifierDbService,
        IGetOrganizationVersionByIdentiferDbService getOrganizationVersionByIdentiferDbService
        )
    {
        _distributedCache = distributedCache;
        _getOrganizationByIdentifierDbService = getOrganizationByIdentifierDbService;
        _getOrganizationVersionByIdentiferDbService = getOrganizationVersionByIdentiferDbService;
    }

    async Task<Result<OrganizationSharedCacheServiceResult>> IServiceHandlerAsync<OrganizationSharedCacheServiceParameter, OrganizationSharedCacheServiceResult>.HandleAsync(OrganizationSharedCacheServiceParameter @params)
    {
        Torganization torganization = null;
        bool isCached = false;
        try
        {
            if (@params is null)
                return ResultExceptionFactory.Error($"{nameof(OrganizationSharedCacheServiceParameter)} object is null", HttpStatusCode.BadRequest);

            if (@params.Identifier is null)
                return ResultExceptionFactory.Error($"{nameof(@params.Identifier)} object is null", HttpStatusCode.BadRequest);

            Guid identifer = @params.Identifier ?? Guid.Empty;

            string cacheName = $"Organization-{identifer}";

            string? cacheValue = await _distributedCache.GetStringAsync(cacheName,@params.CancellationToken)!;

            if (cacheValue is null)
            {
                // Get Organization Data
                var organizationResult = await _getOrganizationByIdentifierDbService
                    .HandleAsync(new GetOrganizationByIdentifierSqlParameter(identifer, @params.CancellationToken));

                if (organizationResult.IsFailed)
                    return ResultExceptionFactory.Error(organizationResult.Errors[0]);

                await SqlCacheHelper.SetCacheAsync(_distributedCache, cacheName, ConstantValue.CacheTime, organizationResult.Value);

                torganization = organizationResult.Value!;
                isCached = true;
            }
            else
            {
                Torganization cacheValueResult = JsonConvert.DeserializeObject<Torganization>(cacheValue!)!;

                if (cacheValueResult is null)
                    return ResultExceptionFactory.Error($"{nameof(Torganization)} object is null", HttpStatusCode.NotFound);

                // Get Row Version
                var organizationVersion = await _getOrganizationVersionByIdentiferDbService
                    .HandleAsync(new GetOrganizationVersionByIdentiferSqlParameter(cacheValueResult.Identifier!, @params.CancellationToken));

                if (organizationVersion.IsFailed)
                    return ResultExceptionFactory.Error(organizationVersion.Errors[0]);

                // Check Row Version
                if (!cacheValueResult.Version.SequenceEqual(organizationVersion.Value))
                {
                    // Get Organization Data
                    var organizationResult = await _getOrganizationByIdentifierDbService
                        .HandleAsync(new GetOrganizationByIdentifierSqlParameter(cacheValueResult.Identifier!, @params.CancellationToken));

                    if (organizationResult.IsFailed)
                        return ResultExceptionFactory.Error(organizationResult.Errors[0]);

                    await SqlCacheHelper.SetCacheAsync(_distributedCache, cacheName, ConstantValue.CacheTime, organizationResult.Value);

                    torganization = organizationResult.Value!;
                    isCached = true;
                }
                else
                {
                    torganization = cacheValueResult;
                    isCached = false;
                }
            }

            OrganizationSharedCacheServiceResult result = new OrganizationSharedCacheServiceResult(torganization, isCached);

            return Result.Ok(result);
        }
        catch (Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}