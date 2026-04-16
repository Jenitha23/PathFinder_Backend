using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace PATHFINDER_BACKEND.Services
{
    public interface ICachingService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task RemoveAsync(string key);
        bool TryGet<T>(string key, out T? value);
        void Set<T>(string key, T value, TimeSpan? expiry = null);
    }

    public class CachingService : ICachingService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingService> _logger;

        public CachingService(IMemoryCache cache, ILogger<CachingService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            return await Task.Run(() => Get<T>(key));
        }

        public T? Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out T? cachedValue))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }
            
            _logger.LogDebug("Cache miss for key: {Key}", key);
            return default;
        }

        public bool TryGet<T>(string key, out T? value)
        {
            var result = _cache.TryGetValue(key, out value);
            if (result)
                _logger.LogDebug("Cache hit for key: {Key}", key);
            else
                _logger.LogDebug("Cache miss for key: {Key}", key);
            return result;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            await Task.Run(() => Set(key, value, expiry));
        }

        public void Set<T>(string key, T value, TimeSpan? expiry = null)
        {
            var options = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(expiry ?? TimeSpan.FromHours(24))
                .SetPriority(CacheItemPriority.Normal);
            
            _cache.Set(key, value, options);
            _logger.LogDebug("Cached value for key: {Key}", key);
        }

        public async Task RemoveAsync(string key)
        {
            await Task.Run(() => _cache.Remove(key));
            _logger.LogDebug("Removed cache for key: {Key}", key);
        }

        public static string MatchCacheKey(int studentId, int jobId) => $"match_{studentId}_{jobId}";
        public static string AtsCacheKey(int studentId, int? jobId = null) => 
            jobId.HasValue ? $"ats_{studentId}_job_{jobId}" : $"ats_{studentId}_standalone";
    }
}