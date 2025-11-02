# Thu?t To�n Newsfeed Ranking

## T?ng Quan

H? th?ng newsfeed c?a SOP API s? d?ng thu?t to�n ranking ??n gi?n, hi?u qu?, v� d? hi?u, ph� h?p cho ??n �n t?t nghi?p. Thu?t to�n k?t h?p hai y?u t? ch�nh: **th?i gian** (recency) v� **m?c ?? t??ng t�c** (engagement) ?? cung c?p n?i dung ???c c� nh�n h�a cho t?ng ng??i d�ng.

## Ki?n Tr�c T?ng Th?

```
???????????????????????????????????????????????????????????????????
?                    GetNewsFeedAsync Flow                        ?
???????????????????????????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 1: Get Followed Users             ?
        ?   - Query user's following list          ?
        ?   - Include user's own ID                ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 2: Query Posts with Ranking       ?
        ?   - Get posts from followed users        ?
        ?   - Posts within last 30 days            ?
        ?   - Calculate ranking score:             ?
        ?     Score = (0.4 � Recency) +            ?
        ?             (0.6 � Engagement)           ?
        ?   - Recency: based on age (0-72 hours)   ?
        ?   - Engagement: likes + (comments � 2)   ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 3: Sort & Paginate                ?
        ?   - Order by ranking score DESC          ?
        ?   - Then by created date DESC            ?
        ?   - Apply pagination (skip/take)         ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 4: Enrich with User Context       ?
        ?   - Check if user liked each post        ?
        ?   - Include ranking score for debugging  ?
        ????????????????????????????????????????????
                              ?
                              ?
        ????????????????????????????????????????????
        ?   Step 5: Build Response                 ?
        ?   - Map to NewsfeedPostModel             ?
        ?   - Include pagination metadata          ?
        ????????????????????????????????????????????
```

---

## Chi Ti?t C�c B??c

### Step 1: Get Followed Users

**M?c ?�ch:** L?y danh s�ch ng??i d�ng m� user ?ang follow.

```csharp
var followedUserIds = await _unitOfWork.FollowerRepository
    .GetQueryable()
    .Where(f => f.FollowerId == userId && !f.IsDeleted)
    .Select(f => f.FollowingId)
    .ToListAsync();

// Th�m ch�nh userId v�o ?? hi?n th? posts c?a m�nh
followedUserIds.Add(userId);
```

---

### Step 2: Query Posts with Optimized Projection

**M?c ?�ch:** L?y posts v?i projection ?? performance t?t nh?t.

**Optimization:**
- **AsNoTracking()**: Kh�ng track changes (read-only)
- **Projection**: Ch? l?y fields c?n thi?t, kh�ng load full collections
- **Single DateTime**: D�ng `currentTime = DateTime.UtcNow` cho t?t c? calculations

**C�ng th?c ranking c?i ti?n:**

```
RankingScore = (0.4 � RecencyScore) + (0.6 � NormalizedEngagement) + SessionShuffle
```

**Trong ?�:**

#### **Recency Score (40%) - Clamped**
```csharp
recencyScore = Math.Max(0, Math.Min(1, 
    (72 - hoursSinceCreated) / 72.0))
```
- **Clamping gi?a 0 v� 1:** Tr�nh gi� tr? �m cho posts > 72 gi?
- Post m?i nh?t: score = 1.0
- Post 36 gi?: score = 0.5
- Post 72 gi?: score = 0.0
- Post > 72 gi?: score = 0.0 (clamped, kh�ng ph?i �m)

#### **Engagement Score (60%) - Log-scale Normalized**
```csharp
rawEngagement = likes + (comments � 2)
normalizedEngagement = Math.Log(1 + rawEngagement)
```
- **Log-scale normalization:** Gi?m dominance c?a viral posts
- **log(1 + x):** Handle zero engagement gracefully
- **V� d?:**
  - 0 engagement: log(1) = 0
  - 10 engagement: log(11) = 2.4
  - 100 engagement: log(101) = 4.6
  - 1000 engagement: log(1001) = 6.9
- **Hi?u qu?:** Posts v?a v?i engagement v?n ???c rank cao, kh�ng b? viral posts ch�n v�i

#### **Session Shuffle (T�y ch?n)**
```csharp
if (!string.IsNullOrEmpty(sessionId))
{
    hashCode = (sessionId + postId).GetHashCode()
    shuffleFactor = (hashCode % 100) / 10000.0  // ±0.01
    rankingScore += shuffleFactor
}
```
- **Deterministic shuffle:** M?i session th?y th? t? kh�c nhau m?t ch�t
- **Nh?ng stable:** C�ng sessionId s? lu�n th?y c�ng th? t?
- **Small adjustment:** Ch? ±0.01 ?? kh�ng l�m lo?n ranking ch�nh

**T?i sao ch?n t? tr?ng 40/60?**
- **40% Recency:** ?? ?m b?o posts m?i v?n ???c ?u ti�n
- **60% Engagement:** ?? n?i dung ch?t l??ng cao kh�ng b? ch�n v�i qu� nhanh
- C�n b?ng gi?a s? m?i m? v� ch?t l??ng
- **Log-scale gi�p:** Viral posts (1000+ likes) kh�ng "ch?t" posts v?a (10-50 likes)

**Query t?i ?u v?i Projection:**

```csharp
// Single timestamp for consistency
var currentTime = DateTime.UtcNow;
var lookbackDate = currentTime.AddDays(-30);

var postsQuery = _unitOfWork.PostRepository
    .GetQueryable()
    .AsNoTracking()  // Read-only, faster
    .Where(p => !p.IsDeleted 
        && p.UserId.HasValue
        && followedUserIds.Contains(p.UserId.Value)
        && p.CreatedDate >= lookbackDate)
    .Select(p => new  // Projection - ch? l?y fields c?n thi?t
    {
        PostId = p.Id,
        UserId = p.UserId ?? 0,
        Body = p.Body,
        CreatedDate = p.CreatedDate,
        UserDisplayName = p.User != null ? p.User.DisplayName : "Unknown",
        AuthorAvatarUrl = p.User != null ? p.User.AvtUrl : null,
        
        // Ch? l?y COUNT, kh�ng load full collections
        LikeCount = p.LikePosts.Count(lp => !lp.IsDeleted),
        CommentCount = p.CommentPosts.Count(cp => !cp.IsDeleted),
        
        Images = p.PostImages.Select(pi => pi.ImgUrl).ToList(),
        Hashtags = p.PostHashtags.Select(ph => ph.Hashtag.Name).ToList(),
        
        // Calculate hours for ranking
        HoursSinceCreation = EF.Functions.DateDiffHour(p.CreatedDate, currentTime)
    });

// Materialize to apply ranking in memory
var posts = await postsQuery.ToListAsync();

// Apply ranking with clamping and normalization
var rankedPosts = posts.Select(p => {
    var recency = Math.Max(0, Math.Min(1, 
        (72 - p.HoursSinceCreation) / 72.0));
    var engagement = Math.Log(1 + p.LikeCount + (p.CommentCount * 2));
    var score = (0.4 * recency) + (0.6 * engagement);
    
    // Optional session shuffle
    if (!string.IsNullOrEmpty(sessionId)) {
        var hash = (sessionId + p.PostId).GetHashCode();
        score += (hash % 100) / 10000.0;
    }
    
    return new { Post = p, RankingScore = score };
});
```

---

### Step 3: Sort & Paginate (In-Memory)

**S?p x?p:**
1. **Ch�nh:** Theo `RankingScore` gi?m d?n
2. **Ph?:** Theo `CreatedDate` gi?m d?n (n?u score b?ng nhau)

**Pagination:**
```csharp
var pagedPosts = rankedPosts
    .OrderByDescending(x => x.RankingScore)
    .ThenByDescending(x => x.Post.CreatedDate)
    .Skip((pageIndex - 1) * pageSize)
    .Take(pageSize)
    .ToList();
```

**L?u �:** Ranking ???c apply in-memory sau khi fetch t? DB v� clamping/normalization c?n c�c operations kh�ng support trong SQL.

---

### Step 4: Enrich with User Context

**M?c ?�ch:** Th�m th�ng tin ng? c?nh c? th? cho user.

```csharp
// L?y danh s�ch posts m� user ?� like
var likedPostIds = await _unitOfWork.LikePostRepository
    .GetQueryable()
    .AsNoTracking()  // Read-only
    .Where(lp => lp.UserId == userId && postIds.Contains(lp.PostId) && !lp.IsDeleted)
    .Select(lp => lp.PostId)
    .ToListAsync();

// Build compact response - no AutoMapper
var feedModels = pagedPosts.Select(x => {
    var p = x.Post;
    return new NewsfeedPostModel {
        Id = p.PostId,
        UserId = p.UserId,
        UserDisplayName = p.UserDisplayName,
        Body = p.Body,
        CreatedAt = p.CreatedDate,
        Hashtags = p.Hashtags.Where(h => !string.IsNullOrEmpty(h)).ToList(),
        Images = p.Images.Where(i => !string.IsNullOrEmpty(i)).ToList(),
        LikeCount = p.LikeCount,
        CommentCount = p.CommentCount,
        IsLikedByUser = likedPostIds.Contains(p.PostId),
        AuthorAvatarUrl = p.AuthorAvatarUrl,
        RankingScore = x.RankingScore  // For debugging
    };
}).ToList();
```

**L?u �:** Kh�ng d�ng AutoMapper ?? control ch�nh x�c data flow v� optimize performance.

---

### Step 5: Build Response

```csharp
var feedModels = rankedPosts.Select(x =>
{
    var model = _mapper.Map<NewsfeedPostModel>(x.Post);
    model.RankingScore = x.RankingScore;
    model.IsLikedByUser = likedPostIds.Contains(x.Post.Id);
    return model;
}).ToList();

var pagination = new Pagination<NewsfeedPostModel>(
    feedModels,
    totalCount,
    pageIndex,
    pageSize);
```

---

## C?u H�nh Parameters

**Thu?t to�n m?i KH�NG C?N c?u h�nh ph?c t?p!** M?i th? ???c hard-code v?i gi� tr? t?i ?u:

```csharp
// T? tr?ng c? ??nh trong code
const double RECENCY_WEIGHT = 0.4;      // 40%
const double ENGAGEMENT_WEIGHT = 0.6;   // 60%
const int COMMENT_MULTIPLIER = 2;       // Comments = 2x likes
const int LOOKBACK_DAYS = 30;           // Posts trong 30 ng�y
const int RECENCY_WINDOW_HOURS = 72;    // ?�nh gi� recency trong 72h
```

**N?u mu?n t�y ch?nh (kh�ng b?t bu?c):**

T?o file `appsettings.json` v� th�m:

```json
{
  "NewsfeedSettings": {
    "RecencyWeight": 0.4,           // T? tr?ng ?? m?i (0-1)
    "EngagementWeight": 0.6,        // T? tr?ng engagement (0-1)
    "CommentMultiplier": 2,         // Comments = ? � likes
    "LookbackDays": 30,             // S? ng�y look back
    "RecencyWindowHours": 72        // C?a s? ?�nh gi� recency (gi?)
  }
}
```

**? ?� ??N GI?N H?N NHI?U!**

---

## Kh�ng C?n Redis Cache!

Thu?t to�n m?i **KH�NG S? D?NG Redis** cho newsfeed ranking. T?t c? ???c x? l� tr?c ti?p b?ng SQL queries.

**L?i �ch:**
- ? ?? ph?c t?p gi?m 90%
- Kh�ng c?n duy tr� cache invalidation
- Kh�ng c?n lo v? cache consistency
- Kh�ng c?n c�i ??t Redis (n?u ch? d�ng cho newsfeed)
- D? debug v� monitor h?n

---

## Performance Optimizations

### 1. Database Indexes (Quan tr?ng!)

**B?T BU?C ph?i c�:**

```sql
-- Index cho query posts by followed users + date
CREATE INDEX idx_post_userid_created 
ON Post(UserId, CreatedDate DESC) 
WHERE IsDeleted = 0;

-- Index cho follower lookup
CREATE INDEX idx_follower_followerid 
ON Follower(FollowerId, FollowingId) 
WHERE IsDeleted = 0;

-- Index cho like lookup
CREATE INDEX idx_likepost_userid_postid 
ON LikePost(UserId, PostId) 
WHERE IsDeleted = 0;

-- Index cho comment count
CREATE INDEX idx_commentpost_postid 
ON CommentPost(PostId) 
WHERE IsDeleted = 0;

-- Index cho like count
CREATE INDEX idx_likepost_postid 
ON LikePost(PostId) 
WHERE IsDeleted = 0;
```

### 2. Query Optimization

- S? d?ng `Include()` ?? load related data trong 1 query (eager loading)
- T�nh ranking score tr?c ti?p trong SQL query (kh�ng ph?i l?y h?t v? r?i t�nh)
- Ch? l?y posts trong 30 ng�y g?n nh?t ?? gi?m data scan

### 3. Database Connection Pooling

Entity Framework Core t? ??ng enable connection pooling:

```csharp
// In Startup.cs/Program.cs
services.AddDbContext<SOPDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(60);
        sqlOptions.EnableRetryOnFailure(3);
    })
);
```

---

## Monitoring & Tuning

### Key Metrics ??n Gi?n

1. **Query time:**
   - Target: <500ms cho full newsfeed query
   - Monitor: EF Core logging hoặc Application Insights
   - N?u ch?m: Ki?m tra indexes

2. **Feed quality metrics:**
   - Engagement rate (likes + comments / views)
   - Monitor qua analytics

### A/B Testing (T�y ch?n)

N?u mu?n optimize, c� th? test:

- **Recency vs Engagement weight:** Thay ??i 40/60 th�nh 30/70 ho?c 50/50
- **Comment multiplier:** Test 1.5x, 2x, 3x
- **Lookback window:** Test 14, 21, 30 ng�y

---

## Example Request/Response

### Request
```http
GET /api/v1/posts/feed?userId=123&pageIndex=1&pageSize=10
Headers:
  Authorization: Bearer {token}
```

**Parameters:**
- `userId`: ID c?a user mu?n l?y newsfeed
- `pageIndex`: Trang hi?n t?i (b?t ??u t? 1)
- `pageSize`: S? l??ng posts m?i trang (khuy?n ngh? 10-20)

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
          "rankingScore": 15.8,  // For debugging
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
      "hasPrevious": false
    }
  }
}
```

**Note:** `rankingScore` c� th? l� s? th?p ph�n (0-1) ho?c s? nguy�n (0-100+) t�y thu?c implementation.

---

## ?? ??N GI?N H?N BAN ??U NHI?U!

### So s�nh v?i thu?t to�n c?:

| Khía c?nh | Thu?t to�n c? | Thu?t to�n m?i |
|-----------|---------------|----------------|
| **S? d?ng Redis** | C? (nhi?u layers) | KH�NG |
| **Session tracking** | C? | KH�NG |
| **Parameters c?n config** | 20+ parameters | 0 (ho?c 3-5 n?u mu?n t�y ch?nh) |
| **Y?u t? ranking** | 7 y?u t? ph?c t?p | 2 y?u t? ??n gi?n |
| **Code lines** | ~400 lines | ~80 lines |
| **D? hi?u** | Kh� | D? |
| **D? maintain** | Kh� | D? |
| **Performance** | T?t (v?i Redis) | T?t (v?i indexes) |

### T?i sao ??n gi?n h?n l?i t?t h?n cho ??n �n t?t nghi?p?

1. **D? hi?u:** Sinh vi�n c� th? gi?i th�ch ???c to�n b? logic
2. **D? debug:** Kh�ng c?n lo v? cache inconsistency
3. **D? test:** Kh�ng c?n mock Redis
4. **D? demo:** Ch? c?n database l� ch?y ???c
5. **Th?c t?:** V?n hi?u qu? cho app nh? (?n 100k users)
6. **??y ?? ch?c n?ng:** V?n c� ranking, personalization, pagination

---

## Future Improvements (N?u mu?n m? r?ng)

Sau khi ho�n th�nh ??n �n, c� th? th�m:

### Phase 2 (Kh�ng b?t bu?c)
- [ ] Th�m Redis cache cho candidate set (gi?m load DB)
- [ ] User affinity based on interaction history
- [ ] Trending hashtag boost
- [ ] Diversity penalty (max posts per author)

### Phase 3 (N�ng cao)
- [ ] ML-based personalization
- [ ] Real-time stream processing
- [ ] A/B testing framework

**Nh?ng hi?n t?i, thu?t to�n ??n gi?n ?� ?? t?t cho graduation project!**

---

## Troubleshooting

### Feed load ch?m

**Cause:** Thi?u database indexes  
**Solution:**
```sql
-- Ch?y c�c l?nh CREATE INDEX ? ph?n Performance Optimizations
```

### Feed kh�ng c� posts

**Possible causes:**
1. User ch?a follow ai
2. Ng??i ???c follow ch?a c� posts
3. T?t c? posts ?� qu� 30 ng�y

**Solution:** Ki?m tra data trong database

### Posts kh�ng ???c s?p x?p ?�ng

**Cause:** Ranking score calculation sai  
**Solution:** Ki?m tra DateDiffHour function v� logic t�nh to�n

---

## References

1. **Entity Framework Core:** Query optimization and performance
2. **SQL Server:** Indexing strategies
3. **Real-world social media feeds:** Facebook, Instagram, Twitter (simplified)

---

## Contact

For questions or issues:
- **Team:** SOP Backend Team (3 students)
- **Project:** Smart Outfit Planner - Graduation Project
- **Documentation Version:** 2.0.0 (Simplified)
- **Last Updated:** 2025-11-02
