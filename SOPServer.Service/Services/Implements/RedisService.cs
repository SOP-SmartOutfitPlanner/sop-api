using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            return await _cache.GetStringAsync(key) != null;
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
    }
}
