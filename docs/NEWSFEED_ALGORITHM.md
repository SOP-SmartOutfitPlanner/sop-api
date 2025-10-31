# Thu?t To�n Newsfeed Ranking

## T?ng Quan

H? th?ng newsfeed c?a SOP API s? d?ng thu?t to�n ranking ph?c t?p, l?y c?m h?ng t? Facebook's EdgeRank, ?? cung c?p n?i dung ???c c� nh�n h�a cho t?ng ng??i d�ng. Thu?t to�n k?t h?p nhi?u y?u t? nh? **th?i gian**, **m?c ?? t??ng t�c**, **m?i quan h? ng??i d�ng**, **ch?t l??ng n?i dung**, v� **?a d?ng h�a**.

## Ki?n Tr�c T?ng Th?

```
???????????????????????????????????????????????????????????????????
?                    GetNewsFeedAsync Flow                        ?
???????????????????????????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 1: Get/Build Candidate Set       ?
        ?   - Check Redis cache                    ?
        ?   - If miss: Query DB for posts from     ?
        ?     followed users + own posts           ?
        ?   - Backfill with trending posts         ?
        ?   - Calculate base scores                ?
        ?   - Cache candidates in Redis ZSET       ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 2: Get Seen Posts                 ?
        ?   - Retrieve seen post IDs from Redis    ?
        ?     using userId + sessionId             ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 3: Filter Unseen Candidates       ?
        ?   - Remove already seen posts            ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 4: Re-Rank Posts                  ?
        ?   - Calculate composite scores with:     ?
        ?     � Recency (time-decay)               ?
        ?     � Engagement (likes, comments)       ?
        ?     � Affinity (user-author relationship)?
        ?     � Quality (author reputation)        ?
        ?     � Diversity (author distribution)    ?
        ?     � Negative feedback penalties        ?
        ?     � Contextual boosts                  ?
        ?   - Apply jitter for variance            ?
        ?   - ?-greedy exploration                 ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 5: Apply Pagination               ?
        ?   - Skip + Take based on page params     ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 6: Fetch Full Post Data           ?
        ?   - Load complete post objects with      ?
        ?     User, Images, Hashtags, etc.         ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 7: Enrich with User Context       ?
        ?   - Check if user liked each post        ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 8: Mark as Seen                   ?
        ?   - Add post IDs to seen set in Redis    ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 9: Build Response                 ?
        ?   - Map to NewsfeedPostModel             ?
        ?   - Include pagination metadata          ?
        ?   - Return sessionId for continuity      ?
        ????????????????????????????????????????????
```

---

## Chi Ti?t C�c B??c

### Step 1: Get/Build Candidate Set

**M?c ?�ch:** Thu th?p t?p h?p c�c b�i post c� kh? n?ng hi?n th? cho ng??i d�ng.

**Quy tr�nh:**

1. **Ki?m tra Redis cache:**
   - Key: `feed:candidates:{userId}`
   - N?u t?n t?i ? tr? v? danh s�ch candidates t? cache
   - N?u kh�ng ? x�y d?ng candidate set m?i

2. **X�y d?ng candidate set m?i:**
   ```csharp
   // L?y danh s�ch ng??i d�ng ???c follow
   var followedUserIds = await _unitOfWork.FollowerRepository
       .GetQueryable()
       .Where(f => f.FollowerId == userId && !f.IsDeleted)
       .Select(f => f.FollowingId)
       .ToListAsync();
   
   // Th�m ch�nh userId v�o danh s�ch
   followedUserIds.Add(userId);
   
   // L?y posts trong kho?ng th?i gian lookback
   var lookbackDate = DateTime.UtcNow.AddDays(-CandidateLookbackDays);
   var posts = await _unitOfWork.PostRepository
       .GetQueryable()
       .Where(p => !p.IsDeleted 
           && p.UserId.HasValue
           && followedUserIds.Contains(p.UserId.Value)
           && p.CreatedDate >= lookbackDate)
       .Include(p => p.LikePosts)
       .Include(p => p.CommentPosts)
       .OrderByDescending(p => p.CreatedDate)
       .Take(MaxCandidateFetch)
       .ToListAsync();
   ```

3. **T�nh base score cho m?i post:**
   ```
   BaseScore = (Wr � Recency) + (We � Engagement)
   
   Recency = exp(-? � age_hours)
   Engagement = ?�likes + ?�comments + ?�reshares
   ```

4. **Cache metrics v�o Redis:**
   - Key: `post:{postId}:metrics`
   - L?u: likes, comments, reshares, authorId, createdAt
   - TTL: `MetricsCacheTTL` ph�t

5. **Backfill v?i trending posts:**
   - N?u `candidates.Count < MinCandidates`
   - L?y th�m posts c� engagement cao trong 3 ng�y g?n ?�y
   - Lo?i tr? posts ?� c� trong candidate set

6. **L?u candidates v�o Redis:**
   - Key: `feed:candidates:{userId}`
   - Type: Sorted Set (ZSET)
   - Score: base score
   - TTL: `CandidateCacheTTL` ph�t

---

### Step 2: Get Seen Posts

**M?c ?�ch:** Tr�nh hi?n th? l?i c�c b�i post ?� xem.

```csharp
var seenPosts = await _redisHelper.GetSeenPostsAsync(userId, sessionId);
```

- **Redis Key:** `seen:{userId}:{sessionId}`
- **Type:** SET
- **TTL:** `SeenPostsTTL` ph�t
- **Session ID:** 
  - Client g?i l�n ?? duy tr� session
  - N?u kh�ng c� ? t?o m?i `Guid.NewGuid().ToString("N")`

---

### Step 3: Filter Unseen Candidates

**M?c ?�ch:** Lo?i b? posts ?� xem kh?i danh s�ch candidates.

```csharp
var unseenCandidates = candidates
    .Where(kvp => !seenPosts.Contains(kvp.Key))
    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
```

---

### Step 4: Re-Rank Posts (Ph?n quan tr?ng nh?t)

**M?c ?�ch:** T�nh to�n ?i?m s? cu?i c�ng cho m?i post d?a tr�n nhi?u y?u t?.

#### 4.1. Composite Score Formula

```
FinalScore = Wr�R + We�E + Wa�A + Wc�Q + Wd�D + Wn�N + Wb�B
```

Trong ?�:

| K� hi?u | T�n | M� t? | C�ng th?c |
|---------|-----|-------|-----------|
| **R** | Recency | ?? m?i c?a post | `exp(-? � age_hours)` |
| **E** | Engagement | M?c ?? t??ng t�c | `?�likes + ?�comments + ?�reshares` |
| **A** | Affinity | M?i quan h? user-author | `normalize(w1�pastLikes + w2�pastComments + w3�replies + w4�visits)` |
| **Q** | Quality | Ch?t l??ng t�c gi? | `EMA(author_engagement_rate, 30d)` |
| **D** | Diversity | Penalty ?a d?ng h�a | `-? � over_representation_factor` |
| **N** | Negative Feedback | Penalty ph?n h?i ti�u c?c | `-? � feedback_severity` |
| **B** | Contextual Boost | Boost theo ng? c?nh | `trending_boost + mutual_followers_boost` |

#### 4.2. Chi Ti?t T?ng Component

##### **Recency Score (R)**

```csharp
var recency = Math.Exp(-lambda * ageHours);
```

- **lambda (?):** T?c ?? decay (m?c ??nh: 0.01)
- **ageHours:** Tu?i c?a post t�nh b?ng gi?
- **??c ?i?m:** 
  - Post m?i ? R ? 1.0
  - Post 24h ? R ? 0.79
  - Post 1 tu?n ? R ? 0.02

##### **Engagement Score (E)**

```csharp
var engagement = (alpha * likes) + (beta * comments) + (gamma * reshares);
```

- **alpha (?):** Tr?ng s? likes (m?c ??nh: 1.0)
- **beta (?):** Tr?ng s? comments (m?c ??nh: 2.0)
- **gamma (?):** Tr?ng s? reshares (m?c ??nh: 3.0)
- **� ngh?a:** Comments quan tr?ng h?n likes, reshares quan tr?ng nh?t

##### **Affinity Score (A)**

```csharp
var rawAffinity = (w1 * pastLikes) + (w2 * pastComments) + 
                  (w3 * directReplies) + (w4 * profileVisits);
var affinity = Math.Min(rawAffinity / maxAffinity, 1.0);
```

- **Y?u t? t�nh to�n:**
  - `pastLikes`: S? l?n user like posts c?a author
  - `pastComments`: S? l?n user comment posts c?a author
  - `directReplies`: S? l?n reply tr?c ti?p
  - `profileVisits`: S? l?n xem profile
- **Boost:** N?u user follow author ? +0.3
- **Range:** [0, 1]

##### **Quality Score (Q)**

```csharp
var quality = Math.Clamp(authorEngagementRate, 0.0, 1.0);
```

- **Hi?n t?i:** Placeholder = 0.5
- **Production:** T�nh EMA c?a engagement rate trong 30 ng�y

##### **Diversity Penalty (D)**

```csharp
if (authorPostCount > diversityThreshold)
{
    var overRepresentation = (authorPostCount - diversityThreshold) / diversityThreshold;
    diversity = -delta * overRepresentation;
}
```

- **M?c ?�ch:** Tr�nh feed b? chi?m b?i 1-2 t�c gi?
- **diversityThreshold:** S? post t?i ?a m?i author (m?c ??nh: 3)
- **delta (?):** M?c ?? penalty (m?c ??nh: 0.2)
- **Tracking:** Redis HASH `feed:author_count:{userId}`

##### **Negative Feedback Penalty (N)**

```csharp
var feedbackSeverity = hideCount + (reportCount * 2);
negativeFeedback = -zeta * feedbackSeverity;
```

- **Hi?n t?i:** Placeholder = 0.0
- **Production:** Track user hide/report actions

##### **Contextual Boost (B)**

```csharp
if (hasTrendingHashtag) boost += TrendingBoost;
if (hasMutualFollowers) boost += MutualFollowersBoost;
```

- **Hi?n t?i:** Placeholder = 0.0
- **Production:** Detect trending hashtags, mutual connections

#### 4.3. Apply Jitter

```csharp
var tau = score * (jitterPercent / 100.0);
var epsilon = (random.NextDouble() * 2 - 1) * tau; // Uniform(-?, ?)
score = score + epsilon;
```

- **M?c ?�ch:** Th�m variance ?? feed kh�ng b? stale
- **jitterPercent:** M?c ??nh 1.5%

#### 4.4. ?-greedy Exploration

```csharp
if (random.NextDouble() < exploreRate && ranked.Count > 10)
{
    var exploreCount = Math.Max(1, (int)(ranked.Count * exploreRate));
    // Swap m?t s? top posts v?i lower-ranked posts
}
```

- **exploreRate:** M?c ??nh 10%
- **M?c ?�ch:** Inject content m?i ?? tr�nh filter bubble

---

### Step 5: Apply Pagination

```csharp
var skip = (pageIndex - 1) * pageSize;
var pagedPostIds = rankedPosts
    .Skip(skip)
    .Take(pageSize)
    .ToList();
```

---

### Step 6: Fetch Full Post Data

```csharp
var posts = await _unitOfWork.PostRepository
    .GetQueryable()
    .Where(p => postIds.Contains(p.Id) && !p.IsDeleted)
    .Include(p => p.User)
    .Include(p => p.PostImages)
    .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
    .Include(p => p.LikePosts)
    .Include(p => p.CommentPosts)
    .ToListAsync();
```

- **Maintain order:** S?p x?p theo th? t? trong `postIds`

---

### Step 7: Enrich with User Context

```csharp
var likedPostIds = await _unitOfWork.LikePostRepository
    .GetQueryable()
    .Where(lp => lp.UserId == userId && postIds.Contains(lp.PostId) && !lp.IsDeleted)
    .Select(lp => lp.PostId)
    .ToHashSet();

model.IsLikedByUser = likedPostIds.Contains(post.Id);
```

---

### Step 8: Mark as Seen

```csharp
await _redisHelper.AddSeenPostsAsync(
    userId, 
    sessionId, 
    pagedPostIds.Select(p => p.PostId),
    TimeSpan.FromMinutes(SeenPostsTTL)
);
```

- **Redis Key:** `seen:{userId}:{sessionId}`
- **Type:** SET
- **TTL:** `SeenPostsTTL` ph�t

---

### Step 9: Build Response

```csharp
var feedModels = posts.Select(p =>
{
    var model = _mapper.Map<NewsfeedPostModel>(p);
    model.RankingScore = rankedPost.Score;
    model.IsLikedByUser = likedPostIds.Contains(p.Id);
    return model;
}).ToList();
```

- **Response includes:**
  - Paginated posts
  - Metadata (totalCount, pageSize, etc.)
  - SessionId (cho request ti?p theo)

---

## C?u H�nh Parameters

T?t c? parameters c� th? config trong `appsettings.json`:

```json
{
  "NewsfeedSettings": {
    // Engagement weights
    "Alpha": 1.0,           // Weight for likes
    "Beta": 2.0,            // Weight for comments
    "Gamma": 3.0,           // Weight for reshares
    
    // Affinity weights
    "W1": 1.0,              // Past likes
    "W2": 2.0,              // Past comments
    "W3": 3.0,              // Direct replies
    "W4": 0.5,              // Profile visits
    "MaxAffinity": 100.0,   // Normalization cap
    
    // Composite score weights
    "Wr": 0.3,              // Recency
    "We": 0.25,             // Engagement
    "Wa": 0.2,              // Affinity
    "Wc": 0.1,              // Quality
    "Wd": 0.05,             // Diversity
    "Wn": 0.05,             // Negative feedback
    "Wb": 0.05,             // Contextual boost
    
    // Other parameters
    "Lambda": 0.01,         // Recency decay rate
    "Delta": 0.2,           // Diversity penalty
    "Zeta": 1.0,            // Negative feedback penalty
    "DiversityThreshold": 3,// Max posts per author
    "JitterPercent": 1.5,   // Score variance
    "ExploreRate": 0.1,     // ?-greedy exploration
    
    // Cache settings
    "CandidateLookbackDays": 7,    // How far back to fetch
    "MaxCandidateFetch": 500,      // Max candidates
    "MinCandidates": 100,          // Trigger backfill
    "CandidateCacheTTL": 30,       // Minutes
    "MetricsCacheTTL": 60,         // Minutes
    "SeenPostsTTL": 120,           // Minutes
    
    // Boost values
    "TrendingBoost": 0.1,
    "MutualFollowersBoost": 0.15
  }
}
```

---

## Redis Data Structures

### 1. Candidate Set
```
Key: feed:candidates:{userId}
Type: ZSET (Sorted Set)
Member: postId
Score: baseScore
TTL: CandidateCacheTTL minutes
```

### 2. Post Metrics
```
Key: post:{postId}:metrics
Type: HASH
Fields:
  - likes: int
  - comments: int
  - reshares: int
  - authorId: long
  - createdAt: datetime (ticks)
TTL: MetricsCacheTTL minutes
```

### 3. Seen Posts
```
Key: seen:{userId}:{sessionId}
Type: SET
Members: postId[]
TTL: SeenPostsTTL minutes
```

### 4. Feed Version
```
Key: feed:ver:{userId}
Type: STRING (counter)
Purpose: Invalidate cache on new post/follow
No TTL
```

### 5. Author Counts
```
Key: feed:author_count:{userId}
Type: HASH
Fields: {authorId}: count
TTL: 10 minutes (per ranking session)
```

---

## Cache Invalidation

### Khi n�o invalidate candidate cache?

1. **User creates new post:**
   ```csharp
   await _redisHelper.InvalidateFeedAsync(userId);
   // Invalidate followers' feeds
   foreach (var followerId in followerIds)
   {
       await _redisHelper.InvalidateFeedAsync(followerId);
   }
   ```

2. **User follows/unfollows:**
   ```csharp
   await _redisHelper.InvalidateFeedAsync(userId);
   ```

3. **Post gets deleted:**
   ```csharp
   await _redisHelper.InvalidateFeedAsync(authorId);
   // Invalidate followers' feeds
   ```

---

## Performance Optimizations

### 1. Multi-tier Caching
- **Tier 1:** Candidate set (30 min TTL)
- **Tier 2:** Post metrics (60 min TTL)
- **Tier 3:** Seen posts (120 min TTL)

### 2. Batch Operations
- Fetch all post metrics in parallel
- Single batch query for liked posts

### 3. Database Indexes
```sql
-- Required indexes
CREATE INDEX idx_post_userid_created ON Post(UserId, CreatedDate DESC) WHERE IsDeleted = 0;
CREATE INDEX idx_follower_followerid ON Follower(FollowerId, FollowingId) WHERE IsDeleted = 0;
CREATE INDEX idx_likepost_userid_postid ON LikePost(UserId, PostId) WHERE IsDeleted = 0;
CREATE INDEX idx_commentpost_postid ON CommentPost(PostId) WHERE IsDeleted = 0;
```

### 4. Connection Pooling
- Redis: Use single IConnectionMultiplexer instance
- Database: EF Core connection pooling enabled

---

## Monitoring & Tuning

### Key Metrics

1. **Cache hit rate:**
   - Target: >80% for candidate cache
   - Monitor: Redis INFO stats

2. **Ranking latency:**
   - Target: <200ms for ranking logic
   - Monitor: Application Insights

3. **Database query time:**
   - Target: <100ms for candidate fetch
   - Monitor: EF Core logging

4. **Feed quality metrics:**
   - Click-through rate (CTR)
   - Dwell time
   - Engagement rate (likes/views)

### A/B Testing Parameters

C�c parameters c� th? test:

- **Recency vs Engagement:** Thay ??i Wr, We
- **Diversity threshold:** Test 2, 3, 4 posts/author
- **Jitter amount:** Test 1%, 2%, 3%
- **Explore rate:** Test 5%, 10%, 15%

---

## Example Request/Response

### Request
```http
GET /api/posts/newsfeed?page-index=1&page-size=10
Headers:
  Authorization: Bearer {token}
  X-Session-Id: abc123 (optional)
```

### Response
```json
{
  "statusCode": 200,
  "message": "Get newsfeed successfully",
  "data": {
    "data": {
      "items": [
        {
          "id": 123,
          "userId": 456,
          "userDisplayName": "John Doe",
          "body": "Check out my new outfit!",
          "hashtags": ["fashion", "ootd"],
          "images": ["https://..."],
          "likeCount": 42,
          "commentCount": 8,
          "isLikedByUser": true,
          "authorAvatarUrl": "https://...",
          "rankingScore": 0.8523,  // For debugging
          "createdAt": "2024-01-15T10:30:00Z"
        }
      ],
      "pageIndex": 1,
      "pageSize": 10,
      "totalCount": 156,
      "totalPages": 16,
      "hasPrevious": false,
      "hasNext": true
    },
    "metaData": {
      "totalCount": 156,
      "pageSize": 10,
      "currentPage": 1,
      "totalPages": 16,
      "hasNext": true,
      "hasPrevious": false,
      "sessionId": "abc123"  // Use in next request
    }
  }
}
```

---

## Roadmap & Future Improvements

### Phase 1 (Current)
- ? Basic ranking with recency + engagement
- ? Redis caching for candidates
- ? Seen posts tracking
- ? Diversity enforcement

### Phase 2 (Planned)
- [ ] ML-based affinity scoring
- [ ] Real-time negative feedback tracking
- [ ] Trending hashtag detection
- [ ] Author quality scoring (EMA)

### Phase 3 (Future)
- [ ] Deep learning ranking model
- [ ] Real-time stream processing
- [ ] Collaborative filtering
- [ ] Multi-armed bandit for exploration

---

## Troubleshooting

### Feed is stale / not updating

**Cause:** Candidate cache not invalidated  
**Solution:**
```csharp
await _redisHelper.InvalidateFeedAsync(userId);
```

### Too many posts from one author

**Cause:** Diversity threshold too high  
**Solution:** Lower `DiversityThreshold` in appsettings

### Feed is too repetitive

**Cause:** Jitter too low, explore rate too low  
**Solution:**
- Increase `JitterPercent` (e.g., 3%)
- Increase `ExploreRate` (e.g., 15%)

### Performance issues

**Causes:**
1. Cache miss ? candidate fetch from DB
2. Too many candidates ? slow ranking
3. Missing database indexes

**Solutions:**
1. Increase cache TTL
2. Reduce `MaxCandidateFetch`
3. Add missing indexes

---

## References

1. **Facebook EdgeRank:** Original inspiration for composite scoring
2. **Twitter Timeline Algorithm:** Affinity and engagement factors
3. **Instagram Explore:** Diversity and exploration strategies
4. **Redis Patterns:** Caching strategies and data structures

---

## Contact

For questions or issues with the newsfeed algorithm:
- **Team:** Backend Team
- **Owner:** [Your Name]
- **Documentation Version:** 1.0.0
- **Last Updated:** 2024-01-15
