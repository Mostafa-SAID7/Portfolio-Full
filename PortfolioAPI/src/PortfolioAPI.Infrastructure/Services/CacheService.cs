using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace PortfolioAPI.Infrastructure.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _defaultOptions;

        public CacheService(IDistributedCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));

            // Default cache options: 30 minutes expiration, sliding expiration
            _defaultOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var cachedData = await _cache.GetStringAsync(key);
            if (cachedData == null)
                return default;

            return JsonSerializer.Deserialize<T>(cachedData);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = expiration.HasValue
                ? new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration.Value,
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                }
                : _defaultOptions;

            var serializedData = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serializedData, options);
        }

        public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options)
        {
            var serializedData = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serializedData, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var value = await _cache.GetStringAsync(key);
            return value != null;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
                return cachedValue;

            var value = await factory();
            await SetAsync(key, value, expiration);
            return value;
        }

        public async Task RefreshAsync(string key)
        {
            await _cache.RefreshAsync(key);
        }

        // Specific caching methods for common portfolio operations
        public async Task<IEnumerable<T>> GetListAsync<T>(string key)
        {
            return await GetAsync<IEnumerable<T>>(key) ?? new List<T>();
        }

        public async Task SetListAsync<T>(string key, IEnumerable<T> items, TimeSpan? expiration = null)
        {
            await SetAsync(key, items, expiration);
        }

        public async Task<T?> GetByIdAsync<T>(string entityType, int id)
        {
            var key = $"{entityType}:{id}";
            return await GetAsync<T>(key);
        }

        public async Task SetByIdAsync<T>(string entityType, int id, T value, TimeSpan? expiration = null)
        {
            var key = $"{entityType}:{id}";
            await SetAsync(key, value, expiration);
        }

        public async Task RemoveByIdAsync(string entityType, int id)
        {
            var key = $"{entityType}:{id}";
            await RemoveAsync(key);
        }

        public async Task ClearEntityCacheAsync(string entityType)
        {
            // Note: This is a simplified implementation.
            // In a real-world scenario, you might want to use cache tags or maintain a registry of keys.
            // For now, we'll clear common patterns
            var patterns = new[] { $"{entityType}:", $"list:{entityType}" };

            foreach (var pattern in patterns)
            {
                // Since IDistributedCache doesn't support pattern matching,
                // we'll need to handle this differently or use a different caching strategy
                // For this implementation, we'll skip pattern-based clearing
            }
        }

        public async Task<IEnumerable<T>> GetPagedListAsync<T>(string key, int pageNumber, int pageSize)
        {
            var pagedKey = $"{key}:page{pageNumber}:size{pageSize}";
            return await GetListAsync<T>(pagedKey);
        }

        public async Task SetPagedListAsync<T>(string key, int pageNumber, int pageSize, IEnumerable<T> items, TimeSpan? expiration = null)
        {
            var pagedKey = $"{key}:page{pageNumber}:size{pageSize}";
            await SetListAsync(pagedKey, items, expiration);
        }
    }

    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task RefreshAsync(string key);
        Task<IEnumerable<T>> GetListAsync<T>(string key);
        Task SetListAsync<T>(string key, IEnumerable<T> items, TimeSpan? expiration = null);
        Task<T?> GetByIdAsync<T>(string entityType, int id);
        Task SetByIdAsync<T>(string entityType, int id, T value, TimeSpan? expiration = null);
        Task RemoveByIdAsync(string entityType, int id);
        Task ClearEntityCacheAsync(string entityType);
        Task<IEnumerable<T>> GetPagedListAsync<T>(string key, int pageNumber, int pageSize);
        Task SetPagedListAsync<T>(string key, int pageNumber, int pageSize, IEnumerable<T> items, TimeSpan? expiration = null);
    }
}