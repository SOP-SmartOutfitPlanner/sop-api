# Newsfeed Feature - Technical Documentation

## Overview

This document describes the implementation of a Facebook-like newsfeed feature with sophisticated ranking algorithms, Redis caching, and refresh dynamics.

## Architecture

### Components

1. **PostService.GetNewsFeedAsync** - Main entry point for feed generation
2. **NewsfeedScoringUtils** - Ranking algorithm utilities
3. **NewsfeedRedisHelper** - Redis caching layer
4. **NewsfeedSettings** - Tunable configuration parameters

### Flow Diagram

```
User Request ? Controller ? PostService
                              ?
                        Check Redis Cache
                              ?
                    [Cache Hit] ? Get Candidates
                              ?
                    [Cache Miss] ? Build Candidates from DB
                              ?
                         Apply Ranking
                              ?
                      Filter Seen Posts
                              ?
                        Apply Pagination
                              ?
                      Fetch Full Post Data
                              ?
                      Mark Posts as Seen
                              ?
                        Return Feed
```

## Ranking Algorithm

### Formula

```
Score = wr*R + we*E + wa*A + wc*Q + wd*D + wn*N + wb*B
```

Where:
- **R** = Recency Score (time-decay)
- **E** = Engagement Score (likes, comments, reshares)
- **A** = Affinity Score (user-author relationship)
- **Q** = Quality Score (author reputation)
- **D** = Diversity Penalty (prevent over-representation)
- **N** = Negative Feedback Penalty (user hides/reports)
- **B** = Contextual Boost (trending, mutual connections)

### Detailed Calculations

#### 1. Recency Score (Time-Decay)
```csharp
R = exp(-? * age_hours)
```
- ? (lambda) = decay rate (default: 0.05)
- Higher ? = faster decay
- Recent posts score higher

**Configuration:**
```json
"Lambda": 0.05
```

#### 2. Engagement Score
```csharp
E = ?*likes + ?*comments + ?*reshares
```
- ? (alpha) = like weight (default: 1.0)
- ? (beta) = comment weight (default: 2.0)
- ? (gamma) = reshare weight (default: 3.0)
- Comments valued more than likes, reshares most valuable

**Configuration:**
```json
"Alpha": 1.0,
"Beta": 2.0,
"Gamma": 3.0
```

#### 3. Affinity Score
```csharp
A = normalize(w1*pastLikes + w2*pastComments + w3*directReplies + w4*profileVisits)
```
- Normalized to [0, 1] range
- Based on user's past interactions with author
- Following relationship adds +0.3 boost

**Configuration:**
```json
"W1": 1.0,  // past likes weight
"W2": 2.0,  // past comments weight
"W3": 3.0,  // direct replies weight
"W4": 0.5,  // profile visits weight
"MaxAffinity": 100.0
```

#### 4. Quality Score
```csharp
Q = EMA(author_engagement_rate, 30d) ? [0,1]
```
- Currently simplified to 0.5
- In production: use exponential moving average of author's engagement rate

#### 5. Diversity Penalty
```csharp
D = -? * over_representation_factor
```
- Prevents feed from being dominated by single author
- Applied when author exceeds threshold (default: 3 posts)

**Configuration:**
```json
"Delta": 0.5,
"DiversityThreshold": 3
```

#### 6. Negative Feedback Penalty
```csharp
N = -? * feedback_severity
```
- feedback_severity = hideCount + (reportCount * 2)
- Reports weigh more heavily than hides

**Configuration:**
```json
"Zeta": 1.0
```

#### 7. Contextual Boost
```csharp
B = trendingBoost (if trending hashtag) + mutualBoost (if mutual followers)
```

**Configuration:**
```json
"TrendingHashtagBoost": 2.0,
"MutualFollowersBoost": 1.0
```

### Composite Score Weights

Control the relative importance of each factor:

```json
"Wr": 1.0,   // Recency weight
"We": 1.5,   // Engagement weight (most important)
"Wa": 2.0,   // Affinity weight (very important for personalization)
"Wc": 1.0,   // Quality weight
"Wd": 1.0,   // Diversity weight
"Wn": 1.0,   // Negative feedback weight
"Wb": 0.5    // Contextual boost weight
```

## Facebook-Like Refresh Dynamics

### 1. Jitter (Random Noise)
```csharp
Score' = Score + ?, where ? ~ U(-?, ?)
? = Score * (JitterPercent / 100)
```
- Adds variance: ±1.5% of score by default
- Makes feed feel fresh on each reload

**Configuration:**
```json
"JitterPercent": 1.5
```

### 2. Time-Decay Auto-Update
- Recency score recalculated on each request
- Posts naturally shift ranks as they age
- No explicit refresh needed

### 3. Softmax Sampling (Optional)
```csharp
p_i = exp(Score_i / T) / ? exp(Score_j / T)
```
- Temperature T controls randomness (default: 0.5)
- Lower T = more deterministic
- Higher T = more random exploration

**Configuration:**
```json
"SoftmaxTemperature": 0.5
```

### 4. ?-Greedy Exploration
- Inject 10% of posts from explore/trending pool
- Randomly swaps top-ranked with lower-ranked posts
- Prevents filter bubble

**Configuration:**
```json
"ExploreRate": 0.1
```

### 5. Diversity Enforcement
- Max 3 posts per author in top results (configurable)
- Diversity penalty increases with over-representation
- Ensures variety in feed

### 6. Seen Posts Tracking
- Redis SET tracks recently viewed posts per session
- TTL: 10 minutes (configurable)
- Prevents duplicate posts in same session

**Key Pattern:**
```
seen:{userId}:{sessionId} ? SET of postIds
```

## Redis Caching Strategy

### Cache Layers

#### 1. Candidate Set Cache
**Key:** `feed:candidates:{userId}`  
**Type:** ZSET (sorted set)  
**TTL:** 10 minutes  
**Data:** postId ? baseScore  

**Purpose:**
- Stores pre-scored candidate posts
- Avoids expensive DB queries on each request
- Only base scores (without user-specific factors)

**Invalidation:**
- New post from followed user
- New follow relationship
- Manual refresh

#### 2. Post Metrics Cache
**Key:** `post:{postId}:metrics`  
**Type:** HASH  
**TTL:** 15 minutes  
**Data:** likes, comments, reshares, authorId, createdAt  

**Purpose:**
- Caches engagement metrics
- Reduces JOIN queries
- Real-time updates on new likes/comments

**Update Strategy:**
- Increment on new engagement
- Refresh on metric query miss

#### 3. Seen Posts Tracking
**Key:** `seen:{userId}:{sessionId}`  
**Type:** SET  
**TTL:** 10 minutes  
**Data:** postId list  

**Purpose:**
- Prevents showing same posts in one session
- Session-scoped (multiple tabs = different sessions)
- Short TTL for fresh content

#### 4. Feed Version
**Key:** `feed:ver:{userId}`  
**Type:** STRING (counter)  
**TTL:** None (persistent)  

**Purpose:**
- Invalidation signal
- Incremented on feed-affecting events
- Used for cache busting

#### 5. Author Count (Diversity Tracking)
**Key:** `feed:author_count:{userId}`  
**Type:** HASH (authorId ? count)  
**TTL:** 10 minutes  

**Purpose:**
- Tracks posts per author in current feed
- Enforces diversity constraints
- Temporary per-request lifecycle

### Cache Refresh Flow

```
Request Feed
    ?
Check Candidates Cache
    ?
[HIT] ? Use cached candidates
    ?
Re-rank with:
    - Current time (fresh time-decay)
    - Jitter (random variance)
    - Diversity (author count)
    ?
[MISS] ? Build from DB
    ?
    1. Query followed users' posts
    2. Calculate base scores
    3. Cache candidates (10min TTL)
    4. Cache post metrics (15min TTL)
    5. Backfill with trending if needed
    ?
Return ranked feed
```

### Cache Invalidation Events

#### When to Invalidate:

1. **User Creates Post**
   - Increment feed version for all followers
   - Clear their candidate caches

2. **User Follows Someone**
   - Increment user's feed version
   - Clear user's candidate cache

3. **Post Gets Engagement**
   - Update post metrics cache
   - No candidate cache clear (metrics auto-refresh)

4. **Manual Refresh Request**
   - Optional: clear candidate cache
   - Recalculate with new jitter

## Candidate Set Generation

### Sources

1. **Primary: Followed Users**
   - Posts from users the requesting user follows
   - Most personalized content

2. **Secondary: Own Posts**
   - User's own posts always eligible
   - Ensures user sees their content

3. **Backfill: Trending/Community**
   - If candidate pool < MinCandidates (default: 20)
   - Recent posts with high engagement
   - Prevents empty feeds for new users

**Configuration:**
```json
"MinCandidates": 20,
"MaxCandidateFetch": 500,
"CandidateLookbackDays": 7
```

### Filtering

Excluded posts:
- Soft-deleted posts
- Posts older than lookback window (7 days)
- Blocked/hidden authors
- Privacy-restricted content

## Performance Optimization

### Database Indexes

Recommended indexes:
```sql
CREATE INDEX idx_post_userid_created ON Post(UserId, CreatedDate DESC) WHERE IsDeleted = 0;
CREATE INDEX idx_post_created ON Post(CreatedDate DESC) WHERE IsDeleted = 0;
CREATE INDEX idx_likepost_userid_postid ON LikePost(UserId, PostId) WHERE IsDeleted = 0;
CREATE INDEX idx_follower_followerid ON Follower(FollowerId) WHERE IsDeleted = 0;
```

### Query Optimization

1. **Batch Loading**
   - Load posts with all related data in single query
   - Use Include() for eager loading

2. **Candidate Limiting**
   - MaxCandidateFetch = 500 prevents excessive DB reads
   - Lookback window = 7 days keeps dataset manageable

3. **Projection**
   - Only load required fields for scoring
   - Full post details loaded after ranking

### Redis Tuning

1. **Connection Pooling**
   - Use IConnectionMultiplexer singleton
   - Async/await for non-blocking I/O

2. **Pipeline Operations**
   - Batch Redis commands where possible
   - Use transactions for atomic updates

3. **Memory Management**
   - Set appropriate TTLs
   - Monitor Redis memory usage
   - Use eviction policies (allkeys-lru)

## Configuration Tuning Guide

### Scenario: Emphasize Fresh Content

```json
{
  "Lambda": 0.1,        // Increase decay rate
  "Wr": 2.0,            // Double recency weight
  "We": 1.0,            // Reduce engagement weight
  "CandidateLookbackDays": 3  // Shorter window
}
```

### Scenario: Emphasize Engagement

```json
{
  "Lambda": 0.03,       // Slower decay
  "We": 3.0,            // Triple engagement weight
  "Wr": 0.5,            // Reduce recency weight
  "Alpha": 1.0,
  "Beta": 3.0,          // Prioritize comments
  "Gamma": 5.0          // Highly value reshares
}
```

### Scenario: Strong Personalization

```json
{
  "Wa": 3.0,            // Triple affinity weight
  "W1": 2.0,            // Double past likes weight
  "W2": 4.0,            // Quadruple comments weight
  "MutualFollowersBoost": 2.0  // Boost mutual connections
}
```

### Scenario: High Diversity

```json
{
  "Delta": 1.5,         // Stronger diversity penalty
  "DiversityThreshold": 2,  // Lower threshold
  "ExploreRate": 0.2    // 20% exploration
}
```

## API Usage

### Endpoint

```
GET /api/v1/posts/feed
```

### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | long | Yes | User requesting the feed |
| pageIndex | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 10) |
| sessionId | string | No | Session ID for seen tracking (auto-generated if not provided) |

### Example Request

```http
GET /api/v1/posts/feed?userId=123&pageIndex=1&pageSize=20&sessionId=abc123xyz
```

### Response Format

```json
{
  "statusCode": 200,
  "message": "Newsfeed retrieved successfully",
  "data": {
    "data": {
      "items": [
        {
          "id": 456,
          "userId": 789,
          "userDisplayName": "John Doe",
          "authorAvatarUrl": "https://...",
          "body": "Post content...",
          "hashtags": ["fashion", "style"],
          "images": ["https://..."],
          "createdAt": "2024-01-15T10:30:00Z",
          "updatedAt": null,
          "likeCount": 42,
          "commentCount": 8,
          "isLikedByUser": true,
          "rankingScore": 156.7
        }
      ],
      "pageIndex": 1,
      "pageSize": 20,
      "totalCount": 150,
      "totalPages": 8,
      "hasPrevious": false,
      "hasNext": true
    },
    "metaData": {
      "totalCount": 150,
      "pageSize": 20,
      "currentPage": 1,
      "totalPages": 8,
      "hasNext": true,
      "hasPrevious": false,
      "sessionId": "abc123xyz"
    }
  }
}
```

### Response Fields

#### Post Fields
- `id` - Post ID
- `userId` - Author user ID
- `userDisplayName` - Author display name
- `authorAvatarUrl` - Author avatar URL
- `body` - Post text content
- `hashtags` - List of hashtag strings
- `images` - List of image URLs
- `createdAt` - Post creation timestamp
- `updatedAt` - Last update timestamp (nullable)
- `likeCount` - Total likes
- `commentCount` - Total comments
- `isLikedByUser` - Whether requesting user liked this post
- `rankingScore` - Computed ranking score (for debugging, not displayed to users)

#### Metadata
- `totalCount` - Total posts available in feed
- `pageSize` - Items per page
- `currentPage` - Current page number
- `totalPages` - Total pages available
- `hasNext` - Whether next page exists
- `hasPrevious` - Whether previous page exists
- `sessionId` - Session ID for tracking (use this in subsequent requests)

## Monitoring & Analytics

### Key Metrics to Track

1. **Cache Hit Rate**
   - Target: >80%
   - Formula: cache_hits / (cache_hits + cache_misses)

2. **Average Feed Load Time**
   - Target: <200ms (cache hit), <500ms (cache miss)
   - Measure from request to response

3. **Feed Engagement Rate**
   - clicks / impressions per post
   - Track which scoring factors correlate with engagement

4. **Diversity Score**
   - Average authors per page
   - Distribution of posts per author

5. **Staleness**
   - Average age of posts shown
   - Percentage of posts >24hrs old

### Redis Monitoring Commands

```bash
# Cache hit ratio
INFO stats | grep keyspace

# Memory usage
INFO memory

# Connected clients
INFO clients

# Slow log (queries >10ms)
SLOWLOG GET 10
```

## Troubleshooting

### Issue: Feed is too repetitive

**Symptoms:**
- Same posts appear frequently
- Few authors dominate feed

**Solutions:**
1. Increase `Delta` (diversity penalty)
2. Lower `DiversityThreshold`
3. Increase `ExploreRate`
4. Check seen posts tracking (TTL may be too short)

### Issue: Feed is too stale

**Symptoms:**
- Old posts rank too high
- Fresh content not appearing

**Solutions:**
1. Increase `Lambda` (faster decay)
2. Increase `Wr` (recency weight)
3. Reduce `CandidateLookbackDays`
4. Lower candidate cache TTL

### Issue: Feed is too random/unpredictable

**Symptoms:**
- Users complain about inconsistent ordering
- No clear ranking pattern

**Solutions:**
1. Reduce `JitterPercent`
2. Lower `ExploreRate`
3. Increase `SoftmaxTemperature`
4. Adjust weights to favor stable factors (engagement, affinity)

### Issue: Poor cache hit rate

**Symptoms:**
- High latency
- Redis memory not being utilized

**Solutions:**
1. Increase cache TTLs
2. Check for frequent feed version increments
3. Review invalidation logic
4. Consider pre-warming caches for active users

### Issue: Empty or sparse feeds

**Symptoms:**
- Users see very few posts
- New users have empty feeds

**Solutions:**
1. Lower `MinCandidates` threshold
2. Extend `CandidateLookbackDays`
3. Improve trending backfill query
4. Consider showing community/public posts

## Future Enhancements

### Planned Features

1. **Machine Learning Scoring**
   - Train model on user engagement patterns
   - Personalized weight optimization per user

2. **Real-Time Updates**
   - WebSocket/SignalR for live feed updates
   - Push notifications for high-relevance posts

3. **Content-Based Filtering**
   - Hashtag preferences
   - Image similarity (ML)
   - Text sentiment analysis

4. **A/B Testing Framework**
   - Multiple ranking algorithms
   - User cohort segmentation
   - Engagement metrics comparison

5. **Advanced Diversity**
   - Content type diversity (text, image, video)
   - Topic diversity (clustering)
   - Temporal diversity (time slots)

6. **Collaborative Filtering**
   - "Users like you also liked"
   - Similar user profiles
   - Network effects

7. **Negative Feedback Learning**
   - Hide/block functionality
   - Downrank similar content
   - Category-level preferences

## References

### Academic Papers
- "EdgeRank: Facebook's Newsfeed Algorithm" (2010)
- "Personalized News Recommendation Using Time-Decaying Scoring" (2018)
- "Ranking Algorithms for News Feeds" (2019)

### Code References
- `SOPServer.Service/Services/Implements/PostService.cs` - Main implementation
- `SOPServer.Service/Utils/NewsfeedScoringUtils.cs` - Ranking utilities
- `SOPServer.Service/Utils/NewsfeedRedisHelper.cs` - Caching layer
- `SOPServer.Service/SettingModels/NewsfeedSettings.cs` - Configuration

### External Documentation
- Redis ZSET: https://redis.io/docs/data-types/sorted-sets/
- AutoMapper: https://automapper.org/
- Entity Framework Core: https://docs.microsoft.com/en-us/ef/core/

---

**Document Version:** 1.0  
**Last Updated:** 2024-01-15  
**Maintainer:** SOP Engineering Team
