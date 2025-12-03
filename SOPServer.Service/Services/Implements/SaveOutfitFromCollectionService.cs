using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SaveOutfitFromCollectionModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class SaveOutfitFromCollectionService : ISaveOutfitFromCollectionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SaveOutfitFromCollectionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> SaveOutfitAsync(long userId, SaveOutfitFromCollectionCreateModel model)
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

            // Check if outfit already belongs to the user
            if (outfit.UserId == userId)
            {
                throw new BadRequestException(MessageConstants.OUTFIT_ALREADY_OWNED_BY_USER);
            }

            // Validate collection exists
            var collection = await _unitOfWork.CollectionRepository.GetByIdAsync(model.CollectionId);
            if (collection == null)
            {
                throw new NotFoundException(MessageConstants.COLLECTION_NOT_FOUND);
            }

            // Verify outfit belongs to the collection
            var collectionOutfit = await _unitOfWork.CollectionOutfitRepository.GetQueryable()
                .FirstOrDefaultAsync(co => co.CollectionId == model.CollectionId && co.OutfitId == model.OutfitId && !co.IsDeleted);
            if (collectionOutfit == null)
            {
                throw new BadRequestException(MessageConstants.OUTFIT_NOT_IN_COLLECTION);
            }

            // Check if already saved from this specific collection (active record)
            var exists = await _unitOfWork.SaveOutfitFromCollectionRepository.ExistsAsync(userId, model.OutfitId, model.CollectionId);
            if (exists)
            {
                throw new BadRequestException(MessageConstants.OUTFIT_ALREADY_SAVED_FROM_COLLECTION);
            }

            // Check if outfit is already saved from any other collection
            var existsFromAnyCollection = await _unitOfWork.SaveOutfitFromCollectionRepository.ExistsByUserAndOutfitAsync(userId, model.OutfitId);
            if (existsFromAnyCollection)
            {
                throw new BadRequestException("This outfit has already been saved from another collection");
            }

            // Check if outfit is already saved from any post
            var existsFromAnyPost = await _unitOfWork.SaveOutfitFromPostRepository.ExistsByUserAndOutfitAsync(userId, model.OutfitId);
            if (existsFromAnyPost)
            {
                throw new BadRequestException("This outfit has already been saved from a post");
            }

            // Check if previously saved but deleted (soft delete)
            var existingRecord = await _unitOfWork.SaveOutfitFromCollectionRepository.GetByUserOutfitAndCollectionIncludeDeletedAsync(userId, model.OutfitId, model.CollectionId);

            SaveOutfitFromCollection savedOutfit;
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
                var saveOutfit = _mapper.Map<SaveOutfitFromCollection>(model);
                saveOutfit.UserId = userId;
                saveOutfit.CreatedDate = DateTime.UtcNow;

                await _unitOfWork.SaveOutfitFromCollectionRepository.AddAsync(saveOutfit);
                await _unitOfWork.SaveAsync();

                // Retrieve with navigation properties
                savedOutfit = await _unitOfWork.SaveOutfitFromCollectionRepository.GetByUserOutfitAndCollectionAsync(userId, model.OutfitId, model.CollectionId)
                    ?? throw new NotFoundException(MessageConstants.OUTFIT_NOT_FOUND);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.OUTFIT_SAVED_FROM_COLLECTION_SUCCESS,
                Data = _mapper.Map<SaveOutfitFromCollectionModel>(savedOutfit)
            };
        }

        public async Task<BaseResponseModel> UnsaveOutfitAsync(long userId, long outfitId, long collectionId)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Check BOTH repositories - outfit could be saved from post OR collection
            var savedOutfitFromCollection = await _unitOfWork.SaveOutfitFromCollectionRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OutfitId == outfitId && !s.IsDeleted);

            var savedOutfitFromPost = await _unitOfWork.SaveOutfitFromPostRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OutfitId == outfitId && !s.IsDeleted);

            // If outfit is not saved in either repository, throw error
            if (savedOutfitFromCollection == null && savedOutfitFromPost == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_NOT_SAVED_FROM_COLLECTION);
            }

            // Soft delete from BOTH repositories to ensure complete removal
            if (savedOutfitFromCollection != null)
            {
                savedOutfitFromCollection.IsDeleted = true;
                savedOutfitFromCollection.UpdatedDate = DateTime.UtcNow;
                _unitOfWork.SaveOutfitFromCollectionRepository.UpdateAsync(savedOutfitFromCollection);
            }

            if (savedOutfitFromPost != null)
            {
                savedOutfitFromPost.IsDeleted = true;
                savedOutfitFromPost.UpdatedDate = DateTime.UtcNow;
                _unitOfWork.SaveOutfitFromPostRepository.UpdateAsync(savedOutfitFromPost);
            }

            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_UNSAVED_FROM_COLLECTION_SUCCESS,
                Data = null
            };
        }

        public async Task<BaseResponseModel> GetSavedOutfitsByUserAsync(long userId, PaginationParameter paginationParameter)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var savedOutfits = await _unitOfWork.SaveOutfitFromCollectionRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(s => s.Outfit)
                        .ThenInclude(o => o.User)
                    .Include(s => s.Outfit)
                        .ThenInclude(o => o.OutfitItems)
                            .ThenInclude(oi => oi.Item)
                                .ThenInclude(i => i.Category)
                    .Include(s => s.Outfit)
                        .ThenInclude(o => o.OutfitItems)
                            .ThenInclude(oi => oi.Item)
                                .ThenInclude(i => i.ItemOccasions)
                                    .ThenInclude(io => io.Occasion)
                    .Include(s => s.Outfit)
                        .ThenInclude(o => o.OutfitItems)
                            .ThenInclude(oi => oi.Item)
                                .ThenInclude(i => i.ItemSeasons)
                                    .ThenInclude(ise => ise.Season)
                    .Include(s => s.Outfit)
                        .ThenInclude(o => o.OutfitItems)
                            .ThenInclude(oi => oi.Item)
                                .ThenInclude(i => i.ItemStyles)
                                    .ThenInclude(ist => ist.Style)
                    .Include(s => s.Collection)
                        .ThenInclude(c => c.User),
                filter: s => s.UserId == userId &&
                           (string.IsNullOrEmpty(paginationParameter.Search) ||
                            (s.Outfit.Name != null && s.Outfit.Name.Contains(paginationParameter.Search)) ||
                            (s.Outfit.Description != null && s.Outfit.Description.Contains(paginationParameter.Search)) ||
                            (s.Collection.Title != null && s.Collection.Title.Contains(paginationParameter.Search))),
                orderBy: q => q.OrderByDescending(s => s.CreatedDate));

            var models = _mapper.Map<Pagination<SaveOutfitFromCollectionDetailedModel>>(savedOutfits);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_SAVED_OUTFITS_FROM_COLLECTION_SUCCESS,
                Data = new ModelPaging
                {
                    Data = models,
                    MetaData = new
                    {
                        models.TotalCount,
                        models.PageSize,
                        models.CurrentPage,
                        models.TotalPages,
                        models.HasNext,
                        models.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> CheckIfSavedAsync(long userId, long outfitId, long collectionId)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var isSaved = await _unitOfWork.SaveOutfitFromCollectionRepository.ExistsAsync(userId, outfitId, collectionId);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.CHECK_SAVED_STATUS_SUCCESS,
                Data = new { IsSaved = isSaved }
            };
        }
    }
}
