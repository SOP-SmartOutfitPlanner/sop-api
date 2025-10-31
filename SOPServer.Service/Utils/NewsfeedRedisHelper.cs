using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace SOPServer.Service.Utils
{
    /// <summary>
    /// Redis helper utilities for newsfeed caching and management.
    /// Handles candidate sets, metrics caching, seen posts tracking, and feed versioning.
    /// </summary>
    public class NewsfeedRedisHelper
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public NewsfeedRedisHelper(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = redis.GetDatabase();
        }

        #region Key Builders

        /// <summary>
        /// Gets Redis key for feed candidates ZSET.
        /// Stores postId -> baseScore mapping.
        /// </summary>
        public static string GetCandidatesKey(long userId) => $"feed:candidates:{userId}";

        /// <summary>
        /// Gets Redis key for post metrics HASH.
        /// Stores likes, comments, authorId, createdAt, etc.
        /// </summary>
        public static string GetPostMetricsKey(long postId) => $"post:{postId}:metrics";

        /// <summary>
        /// Gets Redis key for seen posts SET.
        /// Tracks recently viewed posts per user session.
        /// </summary>
        public static string GetSeenPostsKey(long userId, string sessionId) => $"seen:{userId}:{sessionId}";

        /// <summary>
        /// Gets Redis key for feed version.
        /// Incremented when user's feed needs invalidation (new post, follow, etc).
        /// </summary>
        public static string GetFeedVersionKey(long userId) => $"feed:ver:{userId}";

        /// <summary>
        /// Gets Redis key for cached ranked window (optional short-lived cache).
        /// </summary>
        public static string GetRankedWindowKey(long userId) => $"feed:window:{userId}";

        /// <summary>
        /// Gets Redis key for author post count in current feed.
        /// Used for diversity enforcement.
        /// </summary>
        public static string GetAuthorCountKey(long userId) => $"feed:author_count:{userId}";

        #endregion

        #region Candidate Set Operations

        /// <summary>
        /// Adds multiple post candidates with base scores to Redis ZSET.
        /// </summary>
        public async Task SetCandidatesAsync(long userId, Dictionary<long, double> postScores, TimeSpan expiry)
        {
            var key = GetCandidatesKey(userId);
            var entries = postScores.Select(kvp => new SortedSetEntry(kvp.Key, kvp.Value)).ToArray();
            
            if (entries.Any())
            {
                await _db.SortedSetAddAsync(key, entries);
                await _db.KeyExpireAsync(key, expiry);
            }
        }

        /// <summary>
        /// Gets all candidates for a user from Redis ZSET.
        /// Returns postIds with their base scores.
        /// </summary>
        public async Task<Dictionary<long, double>> GetCandidatesAsync(long userId)
        {
            var key = GetCandidatesKey(userId);
            var entries = await _db.SortedSetRangeByScoreWithScoresAsync(key, order: Order.Descending);
            
            return entries.ToDictionary(
                e => (long)e.Element,
                e => e.Score
            );
        }

        /// <summary>
        /// Checks if candidate set exists for user.
        /// </summary>
        public async Task<bool> CandidatesExistAsync(long userId)
        {
            var key = GetCandidatesKey(userId);
            return await _db.KeyExistsAsync(key);
        }

        /// <summary>
        /// Removes candidate set for user.
        /// </summary>
        public async Task DeleteCandidatesAsync(long userId)
        {
            var key = GetCandidatesKey(userId);
            await _db.KeyDeleteAsync(key);
        }

        #endregion

        #region Post Metrics Operations

        /// <summary>
        /// Caches post metrics in Redis HASH.
        /// </summary>
        public async Task SetPostMetricsAsync(long postId, PostMetricsCache metrics, TimeSpan expiry)
        {
            var key = GetPostMetricsKey(postId);
            var entries = new HashEntry[]
            {
                new HashEntry("likes", metrics.Likes),
                new HashEntry("comments", metrics.Comments),
                new HashEntry("reshares", metrics.Reshares),
                new HashEntry("authorId", metrics.AuthorId),
                new HashEntry("createdAt", metrics.CreatedAt.Ticks)
            };

            await _db.HashSetAsync(key, entries);
            await _db.KeyExpireAsync(key, expiry);
        }

        /// <summary>
        /// Gets post metrics from Redis HASH.
        /// </summary>
        public async Task<PostMetricsCache?> GetPostMetricsAsync(long postId)
        {
            var key = GetPostMetricsKey(postId);
            var entries = await _db.HashGetAllAsync(key);
            
            if (entries.Length == 0)
                return null;

            var dict = entries.ToDictionary(e => e.Name.ToString(), e => e.Value);
            
            return new PostMetricsCache
            {
                Likes = (int)dict["likes"],
                Comments = (int)dict["comments"],
                Reshares = dict.ContainsKey("reshares") ? (int)dict["reshares"] : 0,
                AuthorId = (long)dict["authorId"],
                CreatedAt = new DateTime((long)dict["createdAt"])
            };
        }

        /// <summary>
        /// Increments post engagement metrics (likes or comments).
        /// </summary>
        public async Task IncrementMetricAsync(long postId, string metric, int delta = 1)
        {
            var key = GetPostMetricsKey(postId);
            await _db.HashIncrementAsync(key, metric, delta);
        }

        #endregion

        #region Seen Posts Operations

        /// <summary>
        /// Adds post IDs to seen set for user session.
        /// </summary>
        public async Task AddSeenPostsAsync(long userId, string sessionId, IEnumerable<long> postIds, TimeSpan expiry)
        {
            var key = GetSeenPostsKey(userId, sessionId);
            var values = postIds.Select(id => (RedisValue)id).ToArray();
            
            if (values.Any())
            {
                await _db.SetAddAsync(key, values);
                await _db.KeyExpireAsync(key, expiry);
            }
        }

        /// <summary>
        /// Gets all seen post IDs for user session.
        /// </summary>
        public async Task<HashSet<long>> GetSeenPostsAsync(long userId, string sessionId)
        {
            var key = GetSeenPostsKey(userId, sessionId);
            var values = await _db.SetMembersAsync(key);
            
            return values.Select(v => (long)v).ToHashSet();
        }

        /// <summary>
        /// Checks if specific posts have been seen.
        /// </summary>
        public async Task<bool> IsPostSeenAsync(long userId, string sessionId, long postId)
        {
            var key = GetSeenPostsKey(userId, sessionId);
            return await _db.SetContainsAsync(key, postId);
        }

        #endregion

        #region Feed Version Operations

        /// <summary>
        /// Gets current feed version for user.
        /// </summary>
        public async Task<long> GetFeedVersionAsync(long userId)
        {
            var key = GetFeedVersionKey(userId);
            var version = await _db.StringGetAsync(key);
            return version.HasValue ? (long)version : 0;
        }

        /// <summary>
        /// Increments feed version to invalidate cache.
        /// Called when new post, follow, or other feed-affecting action occurs.
        /// </summary>
        public async Task<long> IncrementFeedVersionAsync(long userId)
        {
            var key = GetFeedVersionKey(userId);
            return await _db.StringIncrementAsync(key);
        }

        /// <summary>
        /// Bumps feed version and clears candidate cache.
        /// </summary>
        public async Task InvalidateFeedAsync(long userId)
        {
            await IncrementFeedVersionAsync(userId);
            await DeleteCandidatesAsync(userId);
            await DeleteRankedWindowAsync(userId);
        }

        #endregion

        #region Ranked Window Operations (Optional Short-Lived Cache)

        /// <summary>
        /// Caches a ranked window of post IDs.
        /// Very short TTL (30-60s) for stale-while-revalidate pattern.
        /// </summary>
        public async Task SetRankedWindowAsync(long userId, List<long> postIds, TimeSpan expiry)
        {
            var key = GetRankedWindowKey(userId);
            var json = JsonSerializer.Serialize(postIds);
            await _db.StringSetAsync(key, json, expiry);
        }

        /// <summary>
        /// Gets cached ranked window.
        /// </summary>
        public async Task<List<long>?> GetRankedWindowAsync(long userId)
        {
            var key = GetRankedWindowKey(userId);
            var json = await _db.StringGetAsync(key);
            
            if (!json.HasValue)
                return null;

            return JsonSerializer.Deserialize<List<long>>(json);
        }

        /// <summary>
        /// Deletes ranked window cache.
        /// </summary>
        public async Task DeleteRankedWindowAsync(long userId)
        {
            var key = GetRankedWindowKey(userId);
            await _db.KeyDeleteAsync(key);
        }

        #endregion

        #region Author Count Operations (Diversity Tracking)

        /// <summary>
        /// Tracks how many posts per author in current feed for diversity enforcement.
        /// </summary>
        public async Task IncrementAuthorCountAsync(long userId, long authorId)
        {
            var key = GetAuthorCountKey(userId);
            await _db.HashIncrementAsync(key, authorId.ToString());
            await _db.KeyExpireAsync(key, TimeSpan.FromMinutes(10));
        }

        /// <summary>
        /// Gets count of posts from specific author in current feed.
        /// </summary>
        public async Task<int> GetAuthorCountAsync(long userId, long authorId)
        {
            var key = GetAuthorCountKey(userId);
            var count = await _db.HashGetAsync(key, authorId.ToString());
            return count.HasValue ? (int)count : 0;
        }

        /// <summary>
        /// Clears author count tracking.
        /// </summary>
        public async Task ClearAuthorCountsAsync(long userId)
        {
            var key = GetAuthorCountKey(userId);
            await _db.KeyDeleteAsync(key);
        }

        #endregion
    }

    /// <summary>
    /// Post metrics cache model for Redis storage.
    /// </summary>
    public class PostMetricsCache
    {
        public int Likes { get; set; }
        public int Comments { get; set; }
        public int Reshares { get; set; }
        public long AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
