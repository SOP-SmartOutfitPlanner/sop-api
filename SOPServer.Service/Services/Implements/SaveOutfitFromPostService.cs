using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SaveOutfitFromPostModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class SaveOutfitFromPostService : ISaveOutfitFromPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SaveOutfitFromPostService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> SaveOutfitAsync(long userId, SaveOutfitFromPostCreateModel model)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Validate outfit exists
            var outfit = await _unitOfWork.OutfitRepository.GetByIdAsync(model.OutfitId);
            if (outfit == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_NOT_FOUND);
            }

            // Validate post exists
            var post = await _unitOfWork.PostRepository.GetByIdAsync(model.PostId);
            if (post == null)
            {
                throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
            }

            // Verify outfit belongs to the post
            var postOutfit = await _unitOfWork.PostOutfitRepository.GetQueryable()
                .FirstOrDefaultAsync(po => po.PostId == model.PostId && po.OutfitId == model.OutfitId && !po.IsDeleted);
            if (postOutfit == null)
            {
                throw new BadRequestException(MessageConstants.OUTFIT_NOT_IN_POST);
            }

            // Check if already saved from this specific post (active record)
            var exists = await _unitOfWork.SaveOutfitFromPostRepository.ExistsAsync(userId, model.OutfitId, model.PostId);
            if (exists)
            {
                throw new BadRequestException(MessageConstants.OUTFIT_ALREADY_SAVED_FROM_POST);
            }

            // Check if outfit is already saved from any other post
            var existsFromAnyPost = await _unitOfWork.SaveOutfitFromPostRepository.ExistsByUserAndOutfitAsync(userId, model.OutfitId);
            if (existsFromAnyPost)
            {
                throw new BadRequestException("This outfit has already been saved from another post");
            }

            // Check if outfit is already saved from any collection
            var existsFromAnyCollection = await _unitOfWork.SaveOutfitFromCollectionRepository.ExistsByUserAndOutfitAsync(userId, model.OutfitId);
            if (existsFromAnyCollection)
            {
                throw new BadRequestException("This outfit has already been saved from a collection");
            }

            // Check if previously saved but deleted (soft delete)
            var existingRecord = await _unitOfWork.SaveOutfitFromPostRepository.GetByUserOutfitAndPostIncludeDeletedAsync(userId, model.OutfitId, model.PostId);

            SaveOutfitFromPost savedOutfit;
            if (existingRecord != null && existingRecord.IsDeleted)
            {
                // Restore the soft-deleted record
                existingRecord.IsDeleted = false;
                existingRecord.UpdatedDate = DateTime.UtcNow;
                await _unitOfWork.SaveAsync();
                savedOutfit = existingRecord;
            }
            else
            {
                // Create new save record
                var saveOutfit = _mapper.Map<SaveOutfitFromPost>(model);
                saveOutfit.UserId = userId;
                saveOutfit.CreatedDate = DateTime.UtcNow;

                await _unitOfWork.SaveOutfitFromPostRepository.AddAsync(saveOutfit);
                await _unitOfWork.SaveAsync();

                // Retrieve with navigation properties
                savedOutfit = await _unitOfWork.SaveOutfitFromPostRepository.GetByUserOutfitAndPostAsync(userId, model.OutfitId, model.PostId)
                    ?? throw new NotFoundException(MessageConstants.OUTFIT_NOT_FOUND);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.OUTFIT_SAVED_FROM_POST_SUCCESS,
                Data = _mapper.Map<SaveOutfitFromPostModel>(savedOutfit)
            };
        }

        public async Task<BaseResponseModel> UnsaveOutfitAsync(long userId, long outfitId, long postId)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Get saved outfit - find from ANY post (post-independent unsave)
            // Since outfits can only be saved once from posts, we find the saved record regardless of postId parameter
            var savedOutfit = await _unitOfWork.SaveOutfitFromPostRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OutfitId == outfitId && !s.IsDeleted);

            if (savedOutfit == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_NOT_SAVED_FROM_POST);
            }

            // Soft delete
            savedOutfit.IsDeleted = true;
            savedOutfit.UpdatedDate = DateTime.UtcNow;
            _unitOfWork.SaveOutfitFromPostRepository.UpdateAsync(savedOutfit);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_UNSAVED_FROM_POST_SUCCESS,
                Data = null
            };
        }

        public async Task<BaseResponseModel> GetSavedOutfitsByUserAsync(long userId)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var savedOutfits = await _unitOfWork.SaveOutfitFromPostRepository.GetByUserIdAsync(userId);
            var result = _mapper.Map<List<SaveOutfitFromPostModel>>(savedOutfits);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_SAVED_OUTFITS_FROM_POST_SUCCESS,
                Data = result
            };
        }

        public async Task<BaseResponseModel> CheckIfSavedAsync(long userId, long outfitId, long postId)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var isSaved = await _unitOfWork.SaveOutfitFromPostRepository.ExistsAsync(userId, outfitId, postId);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.CHECK_SAVED_STATUS_SUCCESS,
                Data = new { IsSaved = isSaved }
            };
        }
    }
}
