using AutoMapper;
using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.LikeCollectionModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class LikeCollectionService : ILikeCollectionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public LikeCollectionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> CreateLikeCollection(CreateLikeCollectionModel model)
        {
            var likeExists = await _unitOfWork.LikeCollectionRepository.GetByUserAndCollection(model.UserId, model.CollectionId);

            LikeCollection likeCollection;
            string message;

            if (likeExists != null)
            {
                // Toggle like status
                likeExists.IsDeleted = !likeExists.IsDeleted;
                _unitOfWork.LikeCollectionRepository.UpdateAsync(likeExists);
                likeCollection = likeExists;
                message = likeExists.IsDeleted ? MessageConstants.UNLIKE_COLLECTION_SUCCESS : MessageConstants.LIKE_COLLECTION_SUCCESS;
            }
            else
            {
                // Create new like
                likeCollection = _mapper.Map<LikeCollection>(model);
                await _unitOfWork.LikeCollectionRepository.AddAsync(likeCollection);
                message = MessageConstants.LIKE_COLLECTION_SUCCESS;
            }

            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = message,
                Data = _mapper.Map<LikeCollectionModel>(likeCollection)
            };
        }
    }
}
