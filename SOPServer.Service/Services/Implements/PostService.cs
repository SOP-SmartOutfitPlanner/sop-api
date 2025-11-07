using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.PostModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly NewsfeedSettings _newsfeedSettings;
        private readonly IRedisService _redisService;

        public PostService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IOptions<NewsfeedSettings> newsfeedSettings,
            IRedisService redisService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _newsfeedSettings = newsfeedSettings.Value;
            _redisService = redisService;
        }

        public async Task<BaseResponseModel> CreatePostAsync(PostCreateModel model)
        {
            await ValidateUserExistsAsync(model.UserId);

            var newPost = await CreatePostEntityAsync(model);
            await AddPostImagesAsync(newPost.Id, model.ImageUrls);
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

        public async Task<BaseResponseModel> GetPostByUserIdAsync(PaginationParameter paginationParameter, long userId)
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
                filter: p => p.UserId == userId,
                orderBy: q => q.OrderByDescending(p => p.CreatedDate)
            );

            var postModels = _mapper.Map<Pagination<PostModel>>(posts);

            return CreatePaginatedResponse(postModels, MessageConstants.GET_LIST_POST_BY_USER_SUCCESS);
        }

        public async Task<BaseResponseModel> GetAllPostsAsync(PaginationParameter paginationParameter, long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
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
                orderBy: q => q.OrderByDescending(p => p.CreatedDate)
            );

            var postModels = _mapper.Map<Pagination<PostModel>>(post);

            // Check if user has liked each post
            foreach (var postModel in postModels)
            {
                var likeExists = await _unitOfWork.LikePostRepository.GetByUserAndPost(userId, postModel.Id);
                postModel.IsLiked = likeExists != null && !likeExists.IsDeleted;
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