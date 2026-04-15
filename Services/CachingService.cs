using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace PATHFINDER_BACKEND.Services
{
    public class CachingService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingService> _logger;

        public CachingService(IMemoryCache cache, ILogger<CachingService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}, computing value", key);
            var value = await factory();

            var options = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(expiry ?? TimeSpan.FromHours(24))
                .SetPriority(CacheItemPriority.Normal);

            _cache.Set(key, value, options);
            return value;
        }

        public static string MatchCacheKey(int studentId, int jobId) => $"match_{studentId}_{jobId}";
        public static string AtsCacheKey(int studentId) => $"ats_{studentId}";
    }
}