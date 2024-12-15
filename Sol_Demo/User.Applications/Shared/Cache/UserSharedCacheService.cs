using Microsoft.Extensions.Caching.Distributed;
using Models.Shared.Constant;
using Newtonsoft.Json;
using Users.Infrastructures.Services.GetUsersByIdentifier;
using Users.Infrastructures.Services.GetVersionByIdentifer;
using Utility.Shared.Cache;

namespace User.Applications.Shared.Cache;

public class UserSharedCacheServiceParameters
{
    public Guid? Identifier { get; }

    public CancellationToken CancellationToken { get; }

    public UserSharedCacheServiceParameters(Guid? identifier, CancellationToken cancellationToken)
    {
        Identifier = identifier;
        CancellationToken = cancellationToken;
    }
}

public class UserSharedCacheServiceResult
{
    public bool? IsCached { get; }

    public GetUserByIdentiferDbServiceResult? GetUserByIdentiferResult { get; }

    public UserSharedCacheServiceResult(bool? isCached, GetUserByIdentiferDbServiceResult? getUserByIdentiferResult)
    {
        IsCached = isCached;
        GetUserByIdentiferResult = getUserByIdentiferResult;
    }
}

public interface IUserSharedCacheService : IServiceHandlerAsync<UserSharedCacheServiceParameters, UserSharedCacheServiceResult>
{
}

[ScopedService(typeof(IUserSharedCacheService))]
public class UserSharedCacheService : IUserSharedCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IGetUserByIdentiferDbService _getUserByIdentiferDbService;
    private readonly IGetUserVersionByIdentifierDbService _getUserVersionByIdentifierDbService;


    public UserSharedCacheService(
        IDistributedCache distributedCache,
        IGetUserByIdentiferDbService getUserByIdentiferDbService,
        IGetUserVersionByIdentifierDbService getUserVersionByIdentifierDbService
        )
    {
        _distributedCache = distributedCache;
        _getUserByIdentiferDbService = getUserByIdentiferDbService;
        _getUserVersionByIdentifierDbService = getUserVersionByIdentifierDbService;
    }

    async Task<Result<UserSharedCacheServiceResult>> IServiceHandlerAsync<UserSharedCacheServiceParameters, UserSharedCacheServiceResult>.HandleAsync(UserSharedCacheServiceParameters @params)
    {
        GetUserByIdentiferDbServiceResult? getUserByIdentiferDbServiceResult = null;
        bool isCached = false;
       try
        {
            if (@params is null)
                return ResultExceptionFactory.Error($"{nameof(UserSharedCacheServiceParameters)} object is null", HttpStatusCode.BadRequest);

            if(@params.Identifier is null)
                return ResultExceptionFactory.Error($"{nameof(@params.Identifier)} object is null", HttpStatusCode.BadRequest);

            Guid identifier = @params.Identifier.Value;

            string cacheName= $"User-{identifier}";

            string? cacheValue=await _distributedCache.GetStringAsync(cacheName, @params.CancellationToken);

            if (cacheValue is null)
            {
                // Get User Data
                var userResult = await _getUserByIdentiferDbService.HandleAsync(new GetUserByIdentiferDbServiceSqlParameters(identifier, @params.CancellationToken));
                if (userResult.IsFailed)
                    return ResultExceptionFactory.Error(userResult.Errors[0]);

                // Set Cache
                getUserByIdentiferDbServiceResult = userResult.Value;
                await SqlCacheHelper.SetCacheAsync(_distributedCache, cacheName, ConstantValue.CacheTime, getUserByIdentiferDbServiceResult);
                isCached = true;
            }
            else
            {
                // Get Cache Value
                GetUserByIdentiferDbServiceResult cacheValueResult = JsonConvert.DeserializeObject<GetUserByIdentiferDbServiceResult>(cacheValue)!;

                if (cacheValueResult is null)
                    return ResultExceptionFactory.Error($"{nameof(cacheValueResult)} object is null", HttpStatusCode.NotFound);

                // Get Row Version
                var versionResult = await _getUserVersionByIdentifierDbService
                    .HandleAsync(new GetUserVersionByIdentifierDbServiceSqlParameters(cacheValueResult.Identifier, @params.CancellationToken));

                if (versionResult.IsFailed)
                    return ResultExceptionFactory.Error(versionResult.Errors[0]);

                // Check Row Version
                if (!cacheValueResult.Version!.SequenceEqual(versionResult.Value))
                {
                    // Get User Data
                    var userResult = await _getUserByIdentiferDbService.HandleAsync(new GetUserByIdentiferDbServiceSqlParameters(cacheValueResult.Identifier, @params.CancellationToken));
                    if (userResult.IsFailed)
                        return ResultExceptionFactory.Error(userResult.Errors[0]);

                    getUserByIdentiferDbServiceResult = userResult.Value;
                    await SqlCacheHelper.SetCacheAsync(_distributedCache, cacheName, ConstantValue.CacheTime, getUserByIdentiferDbServiceResult);
                    isCached = true;
                }
                else
                {
                    getUserByIdentiferDbServiceResult = cacheValueResult;
                    isCached = false;
                }
            }

            UserSharedCacheServiceResult userSharedCacheServiceResult = new UserSharedCacheServiceResult(isCached, getUserByIdentiferDbServiceResult);

            return Result.Ok(userSharedCacheServiceResult);
        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}