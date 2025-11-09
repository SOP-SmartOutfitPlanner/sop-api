using AutoMapper;
using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.LikePostModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.Service.Services.Implements
{
    public class LikePostService : ILikePostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public LikePostService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> CreateLikePost(CreateLikePostModel model)
        {
            var likeExists = await _unitOfWork.LikePostRepository.GetByUserAndPost(model.UserId, model.PostId);

            LikePost likePost;
            string message;

            if (likeExists != null)
            {
                // Toggle like status
                likeExists.IsDeleted = !likeExists.IsDeleted;
                _unitOfWork.LikePostRepository.UpdateAsync(likeExists);
                likePost = likeExists;
                message = likeExists.IsDeleted ? MessageConstants.UNLIKE_POST_SUCCESS : MessageConstants.LIKE_POST_SUCCESS;
            }
            else
            {
                // Create new like
                likePost = _mapper.Map<LikePost>(model);
                await _unitOfWork.LikePostRepository.AddAsync(likePost);
                message = MessageConstants.LIKE_POST_SUCCESS;
            }

            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = message,
                Data = _mapper.Map<LikePostModel>(likePost)
            };
        }
    }
}
