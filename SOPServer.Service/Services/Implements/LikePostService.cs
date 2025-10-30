using AutoMapper;
using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.LikePostModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if(likeExists != null)
            {
                throw new BadRequestException(MessageConstants.ALREADY_LIKE_POST);
            }

            var newLike = _mapper.Map<LikePost>(model);
            await _unitOfWork.LikePostRepository.AddAsync(newLike);
            await _unitOfWork.SaveAsync();
            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.LIKE_POST_SUCCESS,
                Data = _mapper.Map<LikePostModel>(newLike)
            };
        }

        public async Task<BaseResponseModel> DeleteLikePost(int id)
        {
            var likeExists = await _unitOfWork.LikePostRepository.GetByIdAsync(id);
            _unitOfWork.LikePostRepository.SoftDeleteAsync(likeExists);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.UNLIKE_POST_SUCCESS
            };
        }
    }
}
