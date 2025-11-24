using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IRedisService
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<long> IncrementAsync(string key, TimeSpan? expiry = null);
        Task<TimeSpan?> GetTtlAsync(string key);
        Task<(T Value, TimeSpan? Ttl)> GetWithTtlAsync<T>(string key);
        Task<bool> AcquireLockAsync(string key, TimeSpan expiry);
        Task ReleaseLockAsync(string key);
    }
}
