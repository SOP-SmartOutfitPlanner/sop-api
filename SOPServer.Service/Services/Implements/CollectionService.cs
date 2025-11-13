using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.CollectionModels;
using SOPServer.Service.BusinessModels.FirebaseModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.Service.Services.Implements
{
    public class CollectionService : ICollectionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMinioService _minioService;

        public CollectionService(IUnitOfWork unitOfWork, IMapper mapper, IMinioService minioService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _minioService = minioService;
        }

        public async Task<BaseResponseModel> GetAllCollectionsPaginationAsync(PaginationParameter paginationParameter, long? callerUserId)
        {
            var collections = await _unitOfWork.CollectionRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(c => c.User)
                    .Include(c => c.CollectionOutfits.Where(co => !co.IsDeleted))
                        .ThenInclude(co => co.Outfit)
                        .ThenInclude(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                        .ThenInclude(i => i.Category)
                    .Include(x => x.CommentCollections)
                    .Include(x => x.LikeCollections)
                    .Include(x => x.SaveCollections),
                filter: c => c.IsPublished && // Only show published collections
                           (string.IsNullOrWhiteSpace(paginationParameter.Search) ||
                            (c.Title != null && c.Title.Contains(paginationParameter.Search)) ||
                            (c.ShortDescription != null && c.ShortDescription.Contains(paginationParameter.Search))),
                orderBy: x => x.OrderByDescending(x => x.CreatedDate));

            var collectionModels = _mapper.Map<Pagination<CollectionDetailedModel>>(collections);

            // Check following, saved, and liked status if caller user ID is provided
            if (callerUserId.HasValue)
            {
                foreach (var collectionModel in collectionModels)
                {
                    // Check if caller follows the collection author
                    if (collectionModel.UserId.HasValue && collectionModel.UserId.Value != callerUserId.Value)
                    {
                        var isFollowing = await _unitOfWork.FollowerRepository.IsFollowing(callerUserId.Value, collectionModel.UserId.Value);
                        collectionModel.IsFollowing = isFollowing;
                    }
                    else
                    {
                        collectionModel.IsFollowing = false;
                    }

                    // Check if caller saved the collection
                    var savedCollection = await _unitOfWork.SaveCollectionRepository.GetByUserAndCollection(callerUserId.Value, collectionModel.Id);
                    collectionModel.IsSaved = savedCollection != null && !savedCollection.IsDeleted;

                    // Check if caller liked the collection
                    var likedCollection = await _unitOfWork.LikeCollectionRepository.GetByUserAndCollection(callerUserId.Value, collectionModel.Id);
                    collectionModel.IsLiked = likedCollection != null && !likedCollection.IsDeleted;
                }
            }
            else
            {
                foreach (var collectionModel in collectionModels)
                {
                    collectionModel.IsFollowing = false;
                    collectionModel.IsSaved = false;
                    collectionModel.IsLiked = false;
                }
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_COLLECTION_SUCCESS,
                Data = new ModelPaging
                {
                    Data = collectionModels,
                    MetaData = new
                    {
                        collectionModels.TotalCount,
                        collectionModels.PageSize,
                        collectionModels.CurrentPage,
                        collectionModels.TotalPages,
                        collectionModels.HasNext,
                        collectionModels.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetCollectionsByUserPaginationAsync(PaginationParameter paginationParameter, long userId, long? callerUserId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Check if caller is viewing their own collections or someone else's
            bool isOwner = callerUserId.HasValue && callerUserId.Value == userId;

            var collections = await _unitOfWork.CollectionRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(c => c.User)
                    .Include(c => c.CollectionOutfits.Where(co => !co.IsDeleted))
                        .ThenInclude(co => co.Outfit)
                    .Include(c => c.LikeCollections)
                    .Include(c => c.SaveCollections),
                filter: c => c.UserId == userId &&
                           // If owner, show all collections; if not owner, only show published
                           (isOwner || c.IsPublished) &&
                           (string.IsNullOrWhiteSpace(paginationParameter.Search) ||
                            (c.Title != null && c.Title.Contains(paginationParameter.Search)) ||
                            (c.ShortDescription != null && c.ShortDescription.Contains(paginationParameter.Search))),
                orderBy: x => x.OrderByDescending(x => x.CreatedDate));

            var collectionModels = _mapper.Map<Pagination<CollectionModel>>(collections);

            // Check following, saved, and liked status if caller user ID is provided
            if (callerUserId.HasValue)
            {
                foreach (var collectionModel in collectionModels)
                {
                    // Check if caller follows the collection author
                    if (collectionModel.UserId.HasValue && collectionModel.UserId.Value != callerUserId.Value)
                    {
                        var isFollowing = await _unitOfWork.FollowerRepository.IsFollowing(callerUserId.Value, collectionModel.UserId.Value);
                        collectionModel.IsFollowing = isFollowing;
                    }
                    else
                    {
                        collectionModel.IsFollowing = false;
                    }

                    // Check if caller saved the collection
                    var savedCollection = await _unitOfWork.SaveCollectionRepository.GetByUserAndCollection(callerUserId.Value, collectionModel.Id);
                    collectionModel.IsSaved = savedCollection != null && !savedCollection.IsDeleted;

                    // Check if caller liked the collection
                    var likedCollection = await _unitOfWork.LikeCollectionRepository.GetByUserAndCollection(callerUserId.Value, collectionModel.Id);
                    collectionModel.IsLiked = likedCollection != null && !likedCollection.IsDeleted;
                }
            }
            else
            {
                foreach (var collectionModel in collectionModels)
                {
                    collectionModel.IsFollowing = false;
                    collectionModel.IsSaved = false;
                    collectionModel.IsLiked = false;
                }
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_COLLECTION_SUCCESS,
                Data = new ModelPaging
                {
                    Data = collectionModels,
                    MetaData = new
                    {
                        collectionModels.TotalCount,
                        collectionModels.PageSize,
                        collectionModels.CurrentPage,
                        collectionModels.TotalPages,
                        collectionModels.HasNext,
                        collectionModels.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetCollectionByIdAsync(long id, long? callerUserId)
        {
            var collection = await _unitOfWork.CollectionRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(c => c.User)
                    .Include(c => c.CollectionOutfits.Where(co => !co.IsDeleted))
                        .ThenInclude(co => co.Outfit)
                            .ThenInclude(o => o.OutfitItems.Where(oi => !oi.IsDeleted))
                                .ThenInclude(oi => oi.Item)
                                    .ThenInclude(i => i.Category)
                    .Include(c => c.LikeCollections)
                    .Include(c => c.SaveCollections));

            if (collection == null)
            {
                throw new NotFoundException(MessageConstants.COLLECTION_NOT_FOUND);
            }

            var collectionModel = _mapper.Map<CollectionDetailedModel>(collection);

            // Check following, saved, and liked status if caller user ID is provided
            if (callerUserId.HasValue)
            {
                // Check if caller follows the collection author
                if (collectionModel.UserId.HasValue && collectionModel.UserId.Value != callerUserId.Value)
                {
                    var isFollowing = await _unitOfWork.FollowerRepository.IsFollowing(callerUserId.Value, collectionModel.UserId.Value);
                    collectionModel.IsFollowing = isFollowing;
                }
                else
                {
                    collectionModel.IsFollowing = false;
                }

                // Check if caller saved the collection
                var savedCollection = await _unitOfWork.SaveCollectionRepository.GetByUserAndCollection(callerUserId.Value, collectionModel.Id);
                collectionModel.IsSaved = savedCollection != null && !savedCollection.IsDeleted;

                // Check if caller liked the collection
                var likedCollection = await _unitOfWork.LikeCollectionRepository.GetByUserAndCollection(callerUserId.Value, collectionModel.Id);
                collectionModel.IsLiked = likedCollection != null && !likedCollection.IsDeleted;
            }
            else
            {
                collectionModel.IsFollowing = false;
                collectionModel.IsSaved = false;
                collectionModel.IsLiked = false;
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.COLLECTION_GET_SUCCESS,
                Data = collectionModel
            };
        }

        public async Task<BaseResponseModel> CreateCollectionAsync(long userId, CollectionCreateModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (model.Outfits != null && model.Outfits.Any())
            {
                foreach (var outfitInput in model.Outfits)
                {
                    var outfit = await _unitOfWork.OutfitRepository.GetByIdAsync(outfitInput.OutfitId);
                    if (outfit == null)
                    {
                        throw new NotFoundException($"Outfit with ID {outfitInput.OutfitId} not found");
                    }

                    if (outfit.UserId != userId)
                    {
                        throw new ForbiddenException($"Outfit with ID {outfitInput.OutfitId} does not belong to you. You can only add your own outfits to collections.");
                    }
                }
            }

            var thumbnail = await _minioService.UploadImageAsync(model.ThumbnailImg);

            if (thumbnail?.Data is not ImageUploadResult uploadData || string.IsNullOrEmpty(uploadData.DownloadUrl))
            {
                throw new BadRequestException(MessageConstants.FILE_NOT_FOUND);
            }

            var collection = new Collection
            {
                UserId = userId,
                Title = model.Title,
                ShortDescription = model.ShortDescription,
                ThumbnailURL = uploadData.DownloadUrl,
            };

            await _unitOfWork.CollectionRepository.AddAsync(collection);
            await _unitOfWork.SaveAsync();

            if (model.Outfits != null && model.Outfits.Any())
            {
                foreach (var outfitInput in model.Outfits)
                {
                    var collectionOutfit = new CollectionOutfit
                    {
                        CollectionId = collection.Id,
                        OutfitId = outfitInput.OutfitId,
                        Description = outfitInput.Description
                    };
                    await _unitOfWork.CollectionOutfitRepository.AddAsync(collectionOutfit);
                }
                await _unitOfWork.SaveAsync();
            }

            var createdCollection = await _unitOfWork.CollectionRepository.GetByIdIncludeAsync(
                collection.Id,
                include: query => query
                    .Include(c => c.User)
                    .Include(c => c.CollectionOutfits.Where(co => !co.IsDeleted))
                        .ThenInclude(co => co.Outfit)
                            .ThenInclude(o => o.OutfitItems.Where(oi => !oi.IsDeleted))
                                .ThenInclude(oi => oi.Item)
                                    .ThenInclude(i => i.Category));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.COLLECTION_CREATE_SUCCESS,
                Data = _mapper.Map<CollectionDetailedModel>(createdCollection)
            };
        }

        public async Task<BaseResponseModel> UpdateCollectionAsync(long id, long userId, CollectionUpdateModel model)
        {
            var collection = await _unitOfWork.CollectionRepository.GetByIdAsync(id);
            if (collection == null)
            {
                throw new NotFoundException(MessageConstants.COLLECTION_NOT_FOUND);
            }

            if (collection.UserId.HasValue && collection.UserId.Value != userId)
            {
                throw new ForbiddenException(MessageConstants.COLLECTION_ACCESS_DENIED);
            }

            collection.Title = model.Title;
            collection.ShortDescription = model.ShortDescription;

            _unitOfWork.CollectionRepository.UpdateAsync(collection);
            await _unitOfWork.SaveAsync();

            if (model.Outfits != null)
            {
                if (model.Outfits.Any())
                {
                    foreach (var outfitInput in model.Outfits)
                    {
                        var outfit = await _unitOfWork.OutfitRepository.GetByIdAsync(outfitInput.OutfitId);
                        if (outfit == null)
                        {
                            throw new NotFoundException($"Outfit with ID {outfitInput.OutfitId} not found");
                        }

                        if (outfit.UserId != userId)
                        {
                            throw new ForbiddenException($"Outfit with ID {outfitInput.OutfitId} does not belong to you. You can only use your own outfits in collections.");
                        }
                    }
                }

                var collectionExisting = await _unitOfWork.CollectionRepository.GetByIdIncludeAsync(
                    id,
                    include: query => query.Include(c => c.CollectionOutfits));

                var existingOutfits = collectionExisting.CollectionOutfits.Where(co => !co.IsDeleted).ToList();
                _unitOfWork.CollectionOutfitRepository.SoftDeleteRangeAsync(existingOutfits);
                await _unitOfWork.SaveAsync();

                foreach (var outfitInput in model.Outfits)
                {
                    var collectionOutfit = new CollectionOutfit
                    {
                        CollectionId = id,
                        OutfitId = outfitInput.OutfitId,
                        Description = outfitInput.Description
                    };
                    await _unitOfWork.CollectionOutfitRepository.AddAsync(collectionOutfit);
                }

                await _unitOfWork.SaveAsync();
            }

            var updatedCollection = await _unitOfWork.CollectionRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(c => c.User)
                    .Include(c => c.CollectionOutfits.Where(co => !co.IsDeleted))
                        .ThenInclude(co => co.Outfit)
                            .ThenInclude(o => o.OutfitItems.Where(oi => !oi.IsDeleted))
                                .ThenInclude(oi => oi.Item)
                                    .ThenInclude(i => i.Category));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.COLLECTION_UPDATE_SUCCESS,
                Data = _mapper.Map<CollectionDetailedModel>(updatedCollection)
            };
        }

        public async Task<BaseResponseModel> DeleteCollectionAsync(long id, long userId)
        {
            var collection = await _unitOfWork.CollectionRepository.GetByIdAsync(id);
            if (collection == null)
            {
                throw new NotFoundException(MessageConstants.COLLECTION_NOT_FOUND);
            }

            if (collection.UserId.HasValue && collection.UserId.Value != userId)
            {
                throw new ForbiddenException(MessageConstants.COLLECTION_ACCESS_DENIED);
            }

            _unitOfWork.CollectionRepository.SoftDeleteAsync(collection);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.COLLECTION_DELETE_SUCCESS
            };
        }

        public async Task<BaseResponseModel> TogglePublishCollectionAsync(long id, long userId)
        {
            var collection = await _unitOfWork.CollectionRepository.GetByIdAsync(id);
            if (collection == null)
            {
                throw new NotFoundException(MessageConstants.COLLECTION_NOT_FOUND);
            }

            if (collection.UserId.HasValue && collection.UserId.Value != userId)
            {
                throw new ForbiddenException(MessageConstants.COLLECTION_ACCESS_DENIED);
            }

            // Toggle the publish status
            collection.IsPublished = !collection.IsPublished;
            _unitOfWork.CollectionRepository.UpdateAsync(collection);
            await _unitOfWork.SaveAsync();

            var updatedCollection = await _unitOfWork.CollectionRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(c => c.User)
                    .Include(c => c.CollectionOutfits.Where(co => !co.IsDeleted))
                        .ThenInclude(co => co.Outfit)
                            .ThenInclude(o => o.OutfitItems.Where(oi => !oi.IsDeleted))
                                .ThenInclude(oi => oi.Item)
                                    .ThenInclude(i => i.Category));

            var message = collection.IsPublished 
                ? MessageConstants.COLLECTION_PUBLISH_SUCCESS 
                : MessageConstants.COLLECTION_UNPUBLISH_SUCCESS;

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = message,
                Data = _mapper.Map<CollectionDetailedModel>(updatedCollection)
            };
        }
    }
}
