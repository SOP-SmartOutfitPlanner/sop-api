﻿using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
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
        private readonly RedisSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisService(
            IDistributedCache cache,
            IOptions<RedisSettings> settings,
            IConfiguration configuration)
        {
            _cache = cache;
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

            // Serialize and set the value
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
    }
}
