using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Utility.Shared.Cache;

public static class SqlCacheHelper
{
    public static Task SetCacheAsync<TValue>(IDistributedCache distributedCache, string cacheKey, double? cacheTime, TValue value,CancellationToken cancellationToken = default)
    {
        if (distributedCache == null)
            throw new ArgumentNullException(nameof(distributedCache));

        if (cacheKey == null)
            throw new ArgumentNullException(nameof(cacheKey));

        if (cacheTime == null)
            throw new ArgumentNullException(nameof(cacheTime));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var cacheOptions = new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(Convert.ToDouble(cacheTime))
        };

        var jsonData = JsonConvert.SerializeObject(value, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        return distributedCache.SetStringAsync(cacheKey, jsonData, cacheOptions,cancellationToken);
    }

    public static Task<string?> GetCacheAsync(IDistributedCache distributedCache, string cacheKey, CancellationToken cancellationToken = default)
    {
        if (distributedCache == null)
            throw new ArgumentNullException(nameof(distributedCache));

        if (cacheKey == null)
            throw new ArgumentNullException(nameof(cacheKey));

        return distributedCache.GetStringAsync(cacheKey,cancellationToken);
    }

    public static Task RemoveCacheAsync(IDistributedCache distributedCache, string cacheKey, CancellationToken cancellationToken = default)
    {
        if (distributedCache == null)
            throw new ArgumentNullException(nameof(distributedCache));

        if (cacheKey == null)
            throw new ArgumentNullException(nameof(cacheKey));

        return distributedCache.RemoveAsync(cacheKey,cancellationToken);
    }
}