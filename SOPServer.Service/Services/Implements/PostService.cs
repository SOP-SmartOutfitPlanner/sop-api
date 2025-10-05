using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.PostModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PostService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> CreatePostAsync(PostCreateModel model)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(model.UserId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Create new post
            var newPost = new Post
            {
                User = user,
                UserId = model.UserId,
                Body = model.Body
            };

            await _unitOfWork.PostRepository.AddAsync(newPost);
            _unitOfWork.Save();

            foreach (var imageUrl in model.ImageUrls)
            {
                var imagePost = new PostImage
                {
                    PostId = newPost.Id,
                    ImgUrl = imageUrl
                };
                await _unitOfWork.PostImageRepository.AddAsync(imagePost);
            }
            _unitOfWork.Save();


            // Handle hashtags
            if (model.Hashtags != null && model.Hashtags.Any())
            {
                var postHashtagsList = new List<PostHashtags>();

                foreach (var hashtagName in model.Hashtags)
                {
                    if (string.IsNullOrWhiteSpace(hashtagName))
                        continue;

                    // Check if hashtag exists
                    var existingHashtag = await _unitOfWork.HashtagRepository.GetByNameAsync(hashtagName.Trim());
                    
                    Hashtag hashtag;
                    if (existingHashtag == null)
                    {
                        // Create new hashtag
                        hashtag = new Hashtag
                        {
                            Name = hashtagName.Trim()
                        };
                        await _unitOfWork.HashtagRepository.AddAsync(hashtag);
                        _unitOfWork.Save();
                    }
                    else
                    {
                        hashtag = existingHashtag;
                    }

                    // Create post-hashtag relationship
                    var postHashtag = new PostHashtags
                    {
                        PostId = newPost.Id,
                        HashtagId = hashtag.Id
                    };
                    postHashtagsList.Add(postHashtag);
                }

                if (postHashtagsList.Any())
                {
                    await _unitOfWork.PostHashtagsRepository.AddRangeAsync(postHashtagsList);
                    _unitOfWork.Save();
                }
            }

            // Retrieve the created post with all related data
            var createdPost = await _unitOfWork.PostRepository.GetByIdIncludeAsync(
                newPost.Id,
                include: query => query
                    .Include(p => p.User)
                    .Include(p => p.PostImages)
                    .Include(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
            );

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
            _unitOfWork.Save();

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
    }
}
