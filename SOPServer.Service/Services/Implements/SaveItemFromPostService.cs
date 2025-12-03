using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SaveItemFromPostModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class SaveItemFromPostService : ISaveItemFromPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SaveItemFromPostService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> SaveItemAsync(long userId, SaveItemFromPostCreateModel model)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Validate item exists
            var item = await _unitOfWork.ItemRepository.GetByIdAsync(model.ItemId);
            if (item == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_FOUND);
            }

            // Check if item already belongs to the user
            if (item.UserId == userId)
            {
                throw new BadRequestException(MessageConstants.ITEM_ALREADY_OWNED_BY_USER);
            }

            // Validate post exists
            var post = await _unitOfWork.PostRepository.GetByIdAsync(model.PostId);
            if (post == null)
            {
                throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
            }

            // Verify item belongs to the post (either directly or through an outfit)
            var postItem = await _unitOfWork.PostItemRepository.GetQueryable()
                .FirstOrDefaultAsync(pi => pi.PostId == model.PostId && pi.ItemId == model.ItemId && !pi.IsDeleted);

            // If not found directly on post, check if item is in an outfit that belongs to the post
            if (postItem == null)
            {
                var itemInOutfitOfPost = await _unitOfWork.PostOutfitRepository.GetQueryable()
                    .Include(po => po.Outfit)
                        .ThenInclude(o => o.OutfitItems)
                    .AnyAsync(po => po.PostId == model.PostId &&
                                   !po.IsDeleted &&
                                   po.Outfit != null &&
                                   po.Outfit.OutfitItems.Any(oi => oi.ItemId == model.ItemId && !oi.IsDeleted));

                if (!itemInOutfitOfPost)
                {
                    throw new BadRequestException(MessageConstants.ITEM_NOT_IN_POST);
                }
            }

            // Check if already saved from this specific post (active record)
            var exists = await _unitOfWork.SaveItemFromPostRepository.ExistsAsync(userId, model.ItemId, model.PostId);
            if (exists)
            {
                throw new BadRequestException(MessageConstants.ITEM_ALREADY_SAVED);
            }

            // Check if item is already saved from any other post
            var existsFromAnyPost = await _unitOfWork.SaveItemFromPostRepository.ExistsByUserAndItemAsync(userId, model.ItemId);
            if (existsFromAnyPost)
            {
                throw new BadRequestException("This item has already been saved from another post");
            }

            // Check if previously saved but deleted (soft delete)
            var existingRecord = await _unitOfWork.SaveItemFromPostRepository.GetByUserItemAndPostIncludeDeletedAsync(userId, model.ItemId, model.PostId);

            SaveItemFromPost savedItem;
            if (existingRecord != null && existingRecord.IsDeleted)
            {
                // Restore the soft-deleted record
                existingRecord.IsDeleted = false;
                existingRecord.UpdatedDate = DateTime.UtcNow;
                await _unitOfWork.SaveAsync();
                savedItem = existingRecord;
            }
            else
            {
                // Create new save record
                var saveItem = _mapper.Map<SaveItemFromPost>(model);
                saveItem.UserId = userId;
                saveItem.CreatedDate = DateTime.UtcNow;

                await _unitOfWork.SaveItemFromPostRepository.AddAsync(saveItem);
                await _unitOfWork.SaveAsync();

                // Retrieve with navigation properties
                savedItem = await _unitOfWork.SaveItemFromPostRepository.GetByUserItemAndPostAsync(userId, model.ItemId, model.PostId)
                    ?? throw new NotFoundException(MessageConstants.ITEM_NOT_FOUND);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.ITEM_SAVED_SUCCESS,
                Data = _mapper.Map<SaveItemFromPostModel>(savedItem)
            };
        }

        public async Task<BaseResponseModel> UnsaveItemAsync(long userId, long itemId, long postId)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Get saved item - find from ANY post (post-independent unsave)
            // Since items can only be saved once, we find the saved record regardless of postId parameter
            var savedItem = await _unitOfWork.SaveItemFromPostRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ItemId == itemId && !s.IsDeleted);

            if (savedItem == null)
            {
                throw new NotFoundException(MessageConstants.ITEM_NOT_SAVED);
            }

            // Soft delete
            savedItem.IsDeleted = true;
            savedItem.UpdatedDate = DateTime.UtcNow;
            _unitOfWork.SaveItemFromPostRepository.UpdateAsync(savedItem);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.ITEM_UNSAVED_SUCCESS,
                Data = null
            };
        }

        public async Task<BaseResponseModel> GetSavedItemsByUserAsync(long userId, PaginationParameter paginationParameter)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var savedItems = await _unitOfWork.SaveItemFromPostRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(s => s.Item)
                        .ThenInclude(i => i.Category)
                    .Include(s => s.Item)
                        .ThenInclude(i => i.User)
                    .Include(s => s.Item)
                        .ThenInclude(i => i.ItemOccasions)
                            .ThenInclude(io => io.Occasion)
                    .Include(s => s.Item)
                        .ThenInclude(i => i.ItemSeasons)
                            .ThenInclude(ise => ise.Season)
                    .Include(s => s.Item)
                        .ThenInclude(i => i.ItemStyles)
                            .ThenInclude(ist => ist.Style)
                    .Include(s => s.Post)
                        .ThenInclude(p => p.User),
                filter: s => s.UserId == userId &&
                           (string.IsNullOrEmpty(paginationParameter.Search) ||
                            (s.Item.Name != null && s.Item.Name.Contains(paginationParameter.Search)) ||
                            (s.Item.AiDescription != null && s.Item.AiDescription.Contains(paginationParameter.Search)) ||
                            (s.Post.Body != null && s.Post.Body.Contains(paginationParameter.Search))),
                orderBy: q => q.OrderByDescending(s => s.CreatedDate));

            var models = _mapper.Map<Pagination<SaveItemFromPostDetailedModel>>(savedItems);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_SAVED_ITEMS_SUCCESS,
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

        public async Task<BaseResponseModel> CheckIfSavedAsync(long userId, long itemId, long postId)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var isSaved = await _unitOfWork.SaveItemFromPostRepository.ExistsAsync(userId, itemId, postId);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.CHECK_SAVED_STATUS_SUCCESS,
                Data = new { IsSaved = isSaved }
            };
        }
    }
}
