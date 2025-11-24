using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using StackExchange.Redis;
using System.Text.Json;
namespace SOPServer.Service.Services.Implements
{
    public class RedisService : IRedisService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly RedisSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisService(
            IDistributedCache cache,
            IConnectionMultiplexer redis,
            IOptions<RedisSettings> settings)
        {
            _cache = cache;
            _redis = redis;
            _settings = settings.Value;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var value = await _cache.GetStringAsync(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value, _jsonOptions);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(_settings.DefaultExpiryMinutes)
            };

            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value, _jsonOptions), options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var value = await _cache.GetAsync(key);
            return value != null;
        }

        public async Task<long> IncrementAsync(string key, TimeSpan? expiry = null)
        {
            var db = _redis.GetDatabase();
            var value = await db.StringIncrementAsync(key);

            if (expiry.HasValue && value == 1)
            {
                await db.KeyExpireAsync(key, expiry.Value);
            }

            return value;
        }

        public async Task<TimeSpan?> GetTtlAsync(string key)
        {
            var db = _redis.GetDatabase();
            var fullKey = _settings.InstanceName + key;
            var ttl = await db.KeyTimeToLiveAsync(fullKey);
            return ttl;
        }

        public async Task<(T Value, TimeSpan? Ttl)> GetWithTtlAsync<T>(string key)
        {
            var db = _redis.GetDatabase();
            var value = await _cache.GetStringAsync(key);
            if (value == null)
                return (default, null);

            var fullKey = _settings.InstanceName + key;
            var ttl = await db.KeyTimeToLiveAsync(fullKey);
            var deserializedValue = JsonSerializer.Deserialize<T>(value, _jsonOptions);
            return (deserializedValue, ttl);
        }

        public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
        {
            var db = _redis.GetDatabase();
            var lockKey = $"lock:{key}";
            var fullKey = _settings.InstanceName + lockKey;
            return await db.StringSetAsync(fullKey, "locked", expiry, When.NotExists);
        }

        public async Task ReleaseLockAsync(string key)
        {
            var db = _redis.GetDatabase();
            var lockKey = $"lock:{key}";
            var fullKey = _settings.InstanceName + lockKey;
            await db.KeyDeleteAsync(fullKey);
        }
    }
}
