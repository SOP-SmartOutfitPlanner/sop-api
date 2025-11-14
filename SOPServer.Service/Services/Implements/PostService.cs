using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.FirebaseModels;
using SOPServer.Service.BusinessModels.PostModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;

namespace SOPServer.Service.Services.Implements
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly NewsfeedSettings _newsfeedSettings;
        private readonly IRedisService _redisService;
        private readonly IMinioService _minioService;

        public PostService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IOptions<NewsfeedSettings> newsfeedSettings,
            IRedisService redisService,
            IMinioService minioService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _newsfeedSettings = newsfeedSettings.Value;
            _redisService = redisService;
            _minioService = minioService;
        }

        public async Task<BaseResponseModel> CreatePostAsync(PostCreateModel model)
        {
            await ValidateUserExistsAsync(model.UserId);

            // Check if user is suspended
            var suspension = await _unitOfWork.UserSuspensionRepository.GetActiveSuspensionAsync(model.UserId);
            if (suspension != null && suspension.EndAt > DateTime.UtcNow)
            {
                throw new ForbiddenException(
                    $"Your account is suspended until {suspension.EndAt:yyyy-MM-dd HH:mm} UTC. " +
                    $"Reason: {suspension.Reason}. You cannot create posts during this period.");
            }

            var newPost = await CreatePostEntityAsync(model);

            // Upload images to MinIO and get URLs
            var imageUrls = await UploadImagesAsync(model.Images);

            await AddPostImagesAsync(newPost.Id, imageUrls);
            await HandlePostHashtagsAsync(newPost.Id, model.Hashtags);

            var createdPost = await GetPostWithRelationsAsync(newPost.Id);
            var postModel = _mapper.Map<PostModel>(createdPost);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.POST_CREATE_SUCCESS,
                Data = postModel
            };
        }

        public async Task<BaseResponseModel> UpdatePostAsync(long postId, PostUpdateModel model)
        {
            var post = await _unitOfWork.PostRepository.GetByIdIncludeAsync(
                postId,
                include: query => query
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
            );

            if (post == null)
            {
                throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
            }

            post.Body = model.Body;

            // Filter only non-deleted records before passing to update methods
            var activeImages = post.PostImages.Where(img => !img.IsDeleted).ToList();
            var activeHashtags = post.PostHashtags.Where(ph => !ph.IsDeleted).ToList();

            await UpdatePostImagesAsync(post.Id, activeImages, model.Images);
            await UpdatePostHashtagsAsync(post.Id, activeHashtags, model.Hashtags);

            // Update the post entity
            _unitOfWork.PostRepository.UpdateAsync(post);
            await _unitOfWork.SaveAsync();

            var updatedPost = await GetPostWithRelationsAsync(post.Id);
            var postModel = _mapper.Map<PostModel>(updatedPost);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.POST_UPDATE_SUCCESS,
                Data = postModel
            };
        }

        public async Task<BaseResponseModel> DeletePostByIdAsync(long id)
        {
            var post = await _unitOfWork.PostRepository.GetByIdAsync(id);

            if (post == null)
            {
                throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
            }

            _unitOfWork.PostRepository.SoftDeleteAsync(post);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.POST_DELETE_SUCCESS
            };
        }

        public async Task<BaseResponseModel> GetPostByIdAsync(long id)
        {
            var post = await _unitOfWork.PostRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
                    .Include(p => p.LikePosts)
                    .Include(p => p.CommentPosts)
            );

            if (post == null)
            {
                throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
            }

            var postModel = _mapper.Map<PostModel>(post);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.POST_GET_SUCCESS,
                Data = postModel
            };
        }

        public async Task<BaseResponseModel> GetPostByUserIdAsync(PaginationParameter paginationParameter, long userId, long? callerUserId)
        {
            await ValidateUserExistsAsync(userId);

            var posts = await _unitOfWork.PostRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
                    .Include(p => p.LikePosts)
                    .Include(p => p.CommentPosts),
                filter: string.IsNullOrWhiteSpace(paginationParameter.Search)
                    ? p => p.UserId == userId
                    : p => p.UserId == userId && p.Body != null && EF.Functions.Collate(p.Body, "Latin1_General_CI_AI").Contains(EF.Functions.Collate(paginationParameter.Search, "Latin1_General_CI_AI")),
                orderBy: q => q.OrderByDescending(p => p.CreatedDate)
            );

            var postModels = _mapper.Map<Pagination<PostModel>>(posts);

            // Check following status if caller user ID is provided
            if (callerUserId.HasValue)
            {
                foreach (var postModel in postModels)
                {
                    // Don't check if post author is the same as caller
                    if (postModel.UserId != callerUserId.Value)
                    {
                        var isFollowing = await _unitOfWork.FollowerRepository.IsFollowing(callerUserId.Value, postModel.UserId);
                        postModel.IsFollowing = isFollowing;
                    }
                    else
                    {
                        postModel.IsFollowing = false;
                    }
                }
            }
            else
            {
                foreach (var postModel in postModels)
                {
                    postModel.IsFollowing = false;
                }
            }

            return CreatePaginatedResponse(postModels, MessageConstants.GET_LIST_POST_BY_USER_SUCCESS);
        }

        public async Task<BaseResponseModel> GetAllPostsAsync(PaginationParameter paginationParameter, long? callerUserId)
        {
            // Validate user if provided
            if (callerUserId.HasValue)
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(callerUserId.Value);
                if (user == null)
                {
                    throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
                }
            }

            var post = await _unitOfWork.PostRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
                    .Include(p => p.LikePosts)
                    .Include(p => p.CommentPosts),
                filter: string.IsNullOrWhiteSpace(paginationParameter.Search)
                    ? null
                    : p => p.Body != null && EF.Functions.Collate(p.Body, "Latin1_General_CI_AI").Contains(EF.Functions.Collate(paginationParameter.Search, "Latin1_General_CI_AI")),
                orderBy: q => q.OrderByDescending(p => p.CreatedDate)
            );

            var postModels = _mapper.Map<Pagination<PostModel>>(post);

            // Check if user has liked each post and following status
            if (callerUserId.HasValue)
            {
                foreach (var postModel in postModels)
                {
                    var likeExists = await _unitOfWork.LikePostRepository.GetByUserAndPost(callerUserId.Value, postModel.Id);
                    postModel.IsLiked = likeExists != null && !likeExists.IsDeleted;

                    // Don't check if post author is the same as caller
                    if (postModel.UserId != callerUserId.Value)
                    {
                        var isFollowing = await _unitOfWork.FollowerRepository.IsFollowing(callerUserId.Value, postModel.UserId);
                        postModel.IsFollowing = isFollowing;
                    }
                    else
                    {
                        postModel.IsFollowing = false;
                    }
                }
            }
            else
            {
                foreach (var postModel in postModels)
                {
                    postModel.IsLiked = false;
                    postModel.IsFollowing = false;
                }
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_POST_SUCCESS,
                Data = new ModelPaging
                {
                    Data = postModels,
                    MetaData = new
                    {
                        postModels.TotalCount,
                        postModels.PageSize,
                        postModels.CurrentPage,
                        postModels.TotalPages,
                        postModels.HasNext,
                        postModels.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetPostsByHashtagIdAsync(PaginationParameter paginationParameter, long hashtagId)
        {
            // Validate hashtag exists
            var hashtag = await _unitOfWork.HashtagRepository.GetByIdAsync(hashtagId);
            if (hashtag == null)
            {
                throw new NotFoundException($"Hashtag with ID {hashtagId} not found");
            }

            // Get posts that contain the specified hashtag
            var posts = await _unitOfWork.PostRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
                    .Include(p => p.LikePosts)
                    .Include(p => p.CommentPosts),
                filter: p => p.PostHashtags.Any(ph => ph.HashtagId == hashtagId && !ph.IsDeleted),
                orderBy: q => q.OrderByDescending(p => p.CreatedDate)
            );

            var postModels = _mapper.Map<Pagination<PostModel>>(posts);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = $"Get posts by hashtag '{hashtag.Name}' successfully",
                Data = new ModelPaging
                {
                    Data = postModels,
                    MetaData = new
                    {
                        postModels.TotalCount,
                        postModels.PageSize,
                        postModels.CurrentPage,
                        postModels.TotalPages,
                        postModels.HasNext,
                        postModels.HasPrevious,
                        HashtagName = hashtag.Name
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetPostsByHashtagNameAsync(PaginationParameter paginationParameter, string hashtagName, long? callerUserId)
        {
            // Validate hashtag name is not null or empty
            if (string.IsNullOrWhiteSpace(hashtagName))
            {
                throw new BadRequestException("Hashtag name cannot be empty");
            }

            // Validate user if provided
            if (callerUserId.HasValue)
            {
                await ValidateUserExistsAsync(callerUserId.Value);
            }

            // Get hashtag by name (case-insensitive)
            var hashtag = await _unitOfWork.HashtagRepository.GetByNameAsync(hashtagName.Trim());
            if (hashtag == null)
            {
                throw new NotFoundException(MessageConstants.HASHTAG_NOT_FOUND);
            }

            // Get posts that contain the specified hashtag
            var posts = await _unitOfWork.PostRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
                    .Include(p => p.LikePosts)
                    .Include(p => p.CommentPosts),
                filter: p => p.PostHashtags.Any(ph => ph.HashtagId == hashtag.Id && !ph.IsDeleted),
                orderBy: q => q.OrderByDescending(p => p.CreatedDate)
            );

            var postModels = _mapper.Map<Pagination<PostModel>>(posts);

            // Check if user has liked each post and following status
            if (callerUserId.HasValue)
            {
                foreach (var postModel in postModels)
                {
                    var likeExists = await _unitOfWork.LikePostRepository.GetByUserAndPost(callerUserId.Value, postModel.Id);
                    postModel.IsLiked = likeExists != null && !likeExists.IsDeleted;

                    // Don't check if post author is the same as caller
                    if (postModel.UserId != callerUserId.Value)
                    {
                        var isFollowing = await _unitOfWork.FollowerRepository.IsFollowing(callerUserId.Value, postModel.UserId);
                        postModel.IsFollowing = isFollowing;
                    }
                    else
                    {
                        postModel.IsFollowing = false;
                    }
                }
            }
            else
            {
                foreach (var postModel in postModels)
                {
                    postModel.IsLiked = false;
                    postModel.IsFollowing = false;
                }
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_POST_BY_HASHTAG_SUCCESS,
                Data = new ModelPaging
                {
                    Data = postModels,
                    MetaData = new
                    {
                        postModels.TotalCount,
                        postModels.PageSize,
                        postModels.CurrentPage,
                        postModels.TotalPages,
                        postModels.HasNext,
                        postModels.HasPrevious,
                        HashtagName = hashtag.Name
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetTopContributorsAsync(PaginationParameter paginationParameter, long? userId = null)
        {
            if (userId.HasValue)
            {
                await ValidateUserExistsAsync(userId.Value);
            }

            int numberOfDays = 30;//config o cho nay

            var topContributorsQuery = _unitOfWork.PostRepository.GetQueryable()
                .Where(p => p.CreatedDate >= DateTime.UtcNow.AddDays(-numberOfDays) && p.UserId.HasValue)
                .GroupBy(p => new { p.UserId, p.User.DisplayName, p.User.AvtUrl })
                .Select(g => new TopContributorModel
                {
                    UserId = g.Key.UserId.Value,
                    DisplayName = g.Key.DisplayName ?? "Unknown",
                    AvatarUrl = g.Key.AvtUrl ?? string.Empty,
                    PostCount = g.Count()
                })
                .OrderByDescending(c => c.PostCount)
                .AsQueryable();

            var totalCount = await topContributorsQuery.CountAsync();

            var contributors = await topContributorsQuery
                .Skip((paginationParameter.PageIndex - 1) * paginationParameter.PageSize)
                .Take(paginationParameter.PageSize)
                .ToListAsync();

            if (userId.HasValue)
            {
                foreach (var contributor in contributors)
                {
                    var isFollowing = await _unitOfWork.FollowerRepository.IsFollowing(userId.Value, contributor.UserId);
                    contributor.IsFollowing = isFollowing;
                }
            }
            else
            {
                foreach (var contributor in contributors)
                {
                    contributor.IsFollowing = false;
                }
            }

            var pageSize = paginationParameter.TakeAll ? totalCount : paginationParameter.PageSize;
            var pagination = new Pagination<TopContributorModel>(
                contributors,
                totalCount,
                paginationParameter.PageIndex,
                pageSize
            );

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_TOP_CONTRIBUTORS_SUCCESS,
                Data = new ModelPaging
                {
                    Data = pagination,
                    MetaData = new
                    {
                        pagination.TotalCount,
                        pagination.PageSize,
                        pagination.CurrentPage,
                        pagination.TotalPages,
                        pagination.HasNext,
                        pagination.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetPostLikersAsync(PaginationParameter paginationParameter, long postId, long? userId = null)
        {
            // Validate post exists
            var post = await _unitOfWork.PostRepository.GetByIdAsync(postId);
            if (post == null)
            {
                throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
            }

            // Validate user if provided
            if (userId.HasValue)
            {
                await ValidateUserExistsAsync(userId.Value);
            }

            // Get likers with user information
            var likersQuery = _unitOfWork.LikePostRepository.GetQueryable()
                .Where(lp => lp.PostId == postId && !lp.IsDeleted)
                .Include(lp => lp.User)
                .Select(lp => new PostLikerModel
                {
                    UserId = lp.UserId,
                    DisplayName = lp.User.DisplayName ?? "Unknown",
                    AvatarUrl = lp.User.AvtUrl ?? string.Empty,
                    IsFollowing = false
                })
                .AsQueryable();

            var totalCount = await likersQuery.CountAsync();

            var likers = await likersQuery
                .Skip((paginationParameter.PageIndex - 1) * paginationParameter.PageSize)
                .Take(paginationParameter.PageSize)
                .ToListAsync();

            // Check following status if userId is provided
            if (userId.HasValue)
            {
                foreach (var liker in likers)
                {
                    // Don't check if liker is the same as requesting user
                    if (liker.UserId != userId.Value)
                    {
                        var isFollowing = await _unitOfWork.FollowerRepository.IsFollowing(userId.Value, liker.UserId);
                        liker.IsFollowing = isFollowing;
                    }
                }
            }

            var pageSize = paginationParameter.TakeAll ? totalCount : paginationParameter.PageSize;
            var pagination = new Pagination<PostLikerModel>(
                likers,
                totalCount,
                paginationParameter.PageIndex,
                pageSize
            );

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_POST_LIKERS_SUCCESS,
                Data = new ModelPaging
                {
                    Data = pagination,
                    MetaData = new
                    {
                        pagination.TotalCount,
                        pagination.PageSize,
                        pagination.CurrentPage,
                        pagination.TotalPages,
                        pagination.HasNext,
                        pagination.HasPrevious
                    }
                }
            };
        }

        #region Private Helper Methods

        private async Task ValidateUserExistsAsync(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }
        }

        private async Task<Post> CreatePostEntityAsync(PostCreateModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(model.UserId);

            var newPost = new Post
            {
                User = user,
                UserId = model.UserId,
                Body = model.Body
            };

            await _unitOfWork.PostRepository.AddAsync(newPost);
            await _unitOfWork.SaveAsync();

            return newPost;
        }

        private async Task<List<string>> UploadImagesAsync(List<IFormFile> images)
        {
            var imageUrls = new List<string>();

            if (images == null || !images.Any())
            {
                return imageUrls;
            }

            foreach (var image in images)
            {
                if (image == null || image.Length == 0)
                {
                    continue;
                }

                var uploadResult = await _minioService.UploadImageAsync(image);

                if (uploadResult?.Data is ImageUploadResult uploadData && !string.IsNullOrEmpty(uploadData.DownloadUrl))
                {
                    imageUrls.Add(uploadData.DownloadUrl);
                }
            }

            return imageUrls;
        }

        private async Task AddPostImagesAsync(long postId, List<string> imageUrls)
        {
            if (imageUrls == null || !imageUrls.Any())
            {
                return;
            }

            foreach (var imageUrl in imageUrls)
            {
                var imagePost = new PostImage
                {
                    PostId = postId,
                    ImgUrl = imageUrl
                };
                await _unitOfWork.PostImageRepository.AddAsync(imagePost);
            }

            await _unitOfWork.SaveAsync();
        }

        private async Task HandlePostHashtagsAsync(long postId, List<string> hashtags)
        {
            if (hashtags == null || !hashtags.Any())
            {
                return;
            }

            var postHashtagsList = new List<PostHashtags>();

            foreach (var hashtagName in hashtags)
            {
                if (string.IsNullOrWhiteSpace(hashtagName))
                {
                    continue;
                }

                var hashtag = await GetOrCreateHashtagAsync(hashtagName.Trim());

                var postHashtag = new PostHashtags
                {
                    PostId = postId,
                    HashtagId = hashtag.Id
                };
                postHashtagsList.Add(postHashtag);
            }

            if (postHashtagsList.Any())
            {
                await _unitOfWork.PostHashtagsRepository.AddRangeAsync(postHashtagsList);
                await _unitOfWork.SaveAsync();
            }
        }

        private async Task<Hashtag> GetOrCreateHashtagAsync(string hashtagName)
        {
            var existingHashtag = await _unitOfWork.HashtagRepository.GetByNameAsync(hashtagName);

            if (existingHashtag != null)
            {
                return existingHashtag;
            }

            var newHashtag = new Hashtag
            {
                Name = hashtagName
            };

            await _unitOfWork.HashtagRepository.AddAsync(newHashtag);
            await _unitOfWork.SaveAsync();

            return newHashtag;
        }

        private async Task UpdatePostImagesAsync(long postId, List<PostImage> existingImages, List<IFormFile> newImages)
        {
            // Delete old images from MinIO and soft delete from database
            // existingImages already filtered for non-deleted records
            if (existingImages != null && existingImages.Any())
            {
                foreach (var existingImage in existingImages)
                {
                    if (!string.IsNullOrEmpty(existingImage.ImgUrl))
                    {
                        var fileName = ExtractFileNameFromUrl(existingImage.ImgUrl);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            try
                            {
                                await _minioService.DeleteImageAsync(fileName);
                            }
                            catch
                            {
                                // Continue even if delete fails (file might not exist)
                            }
                        }
                    }

                    _unitOfWork.PostImageRepository.SoftDeleteAsync(existingImage);
                }
                await _unitOfWork.SaveAsync();
            }

            // Upload and add new images
            if (newImages != null && newImages.Any())
            {
                var imageUrls = await UploadImagesAsync(newImages);
                await AddPostImagesAsync(postId, imageUrls);
            }
        }

        private async Task UpdatePostHashtagsAsync(long postId, List<PostHashtags> existingHashtags, List<string> newHashtagNames)
        {
            // Handle null newHashtagNames - treat as empty list
            if (newHashtagNames == null)
            {
                newHashtagNames = new List<string>();
            }

            // Get the new hashtag names (trimmed and normalized)
            var normalizedNewHashtags = newHashtagNames
                .Where(h => !string.IsNullOrWhiteSpace(h))
                .Select(h => h.Trim())
                .Distinct()
                .ToList();

            // Get existing hashtag names (already filtered for non-deleted records)
            var existingHashtagNames = existingHashtags
                .Where(ph => ph.Hashtag != null)
                .Select(ph => ph.Hashtag.Name)
                .ToHashSet();

            // Find hashtags to remove (exist but not in new list)
            var hashtagsToRemove = existingHashtags
                .Where(ph => !normalizedNewHashtags.Contains(ph.Hashtag?.Name ?? ""))
                .ToList();

            // Find hashtags to add (in new list but not exist)
            var hashtagsToAdd = normalizedNewHashtags
                .Where(name => !existingHashtagNames.Contains(name))
                .ToList();

            // Remove old hashtags
            foreach (var hashtagToRemove in hashtagsToRemove)
            {
                _unitOfWork.PostHashtagsRepository.SoftDeleteAsync(hashtagToRemove);
            }

            if (hashtagsToRemove.Any())
            {
                await _unitOfWork.SaveAsync();
            }

            // Add new hashtags
            if (hashtagsToAdd.Any())
            {
                await HandlePostHashtagsAsync(postId, hashtagsToAdd);
            }
        }

        private string ExtractFileNameFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            try
            {
                var uri = new Uri(url);
                var segments = uri.Segments;
                // Return the last segment which should be the filename
                return segments.Length > 0 ? segments[segments.Length - 1] : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<Post?> GetPostWithRelationsAsync(long postId)
        {
            return await _unitOfWork.PostRepository.GetByIdIncludeAsync(
                postId,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
                    .Include(p => p.LikePosts)
            );
        }

        private BaseResponseModel CreatePaginatedResponse<T>(Pagination<T> pagination, string message)
        {
            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = message,
                Data = new ModelPaging
                {
                    Data = pagination,
                    MetaData = new
                    {
                        pagination.TotalCount,
                        pagination.PageSize,
                        pagination.CurrentPage,
                        pagination.TotalPages,
                        pagination.HasNext,
                        pagination.HasPrevious
                    }
                }
            };
        }
        #endregion
    }
}