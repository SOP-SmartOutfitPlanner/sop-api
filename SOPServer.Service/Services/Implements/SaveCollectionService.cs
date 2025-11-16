using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.CollectionModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SaveCollectionModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class SaveCollectionService : ISaveCollectionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SaveCollectionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> ToggleSaveCollectionAsync(CreateSaveCollectionModel model)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(model.UserId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Validate collection exists
            var collection = await _unitOfWork.CollectionRepository.GetByIdAsync(model.CollectionId);
            if (collection == null)
            {
                throw new NotFoundException(MessageConstants.COLLECTION_NOT_FOUND);
            }

            var saveExists = await _unitOfWork.SaveCollectionRepository.GetByUserAndCollection(model.UserId, model.CollectionId);

            SaveCollection saveCollection;
            string message;

            if (saveExists != null)
            {
                // Toggle save status
                saveExists.IsDeleted = !saveExists.IsDeleted;
                _unitOfWork.SaveCollectionRepository.UpdateAsync(saveExists);
                saveCollection = saveExists;
                message = saveExists.IsDeleted ? MessageConstants.UNSAVE_COLLECTION_SUCCESS : MessageConstants.SAVE_COLLECTION_SUCCESS;
            }
            else
            {
                // Create new save
                saveCollection = _mapper.Map<SaveCollection>(model);
                await _unitOfWork.SaveCollectionRepository.AddAsync(saveCollection);
                message = MessageConstants.SAVE_COLLECTION_SUCCESS;
            }

            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = message,
                Data = _mapper.Map<SaveCollectionModel>(saveCollection)
            };
        }

        public async Task<BaseResponseModel> GetSavedCollectionsByUserAsync(PaginationParameter paginationParameter, long userId)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var savedCollections = await _unitOfWork.SaveCollectionRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(sc => sc.Collection)
                        .ThenInclude(c => c.User)
                    .Include(sc => sc.Collection)
                        .ThenInclude(c => c.CollectionOutfits.Where(co => !co.IsDeleted))
                            .ThenInclude(co => co.Outfit)
                    .Include(sc => sc.Collection)
                        .ThenInclude(c => c.LikeCollections)
                    .Include(sc => sc.Collection)
                        .ThenInclude(c => c.CommentCollections),
                    // REMOVED: .Include(sc => sc.Collection).ThenInclude(c => c.SaveCollections) - causes circular reference
                filter: sc => sc.UserId == userId,
                orderBy: q => q.OrderByDescending(sc => sc.CreatedDate)
            );

            // Map saved collections to CollectionModel
            var collectionModels = new List<CollectionModel>();

            foreach (var sc in savedCollections)
            {
                var collectionModel = _mapper.Map<CollectionModel>(sc.Collection);
                
                // Manually calculate SavedCount to avoid circular reference
                collectionModel.SavedCount = await _unitOfWork.SaveCollectionRepository
                    .GetQueryable()
                    .Where(save => save.CollectionId == sc.Collection.Id && !save.IsDeleted)
                    .CountAsync();
                
                // Check if caller follows the collection author
                if (collectionModel.UserId.HasValue && collectionModel.UserId.Value != userId)
                {
                    var isFollowing = await _unitOfWork.FollowerRepository.IsFollowing(userId, collectionModel.UserId.Value);
                    collectionModel.IsFollowing = isFollowing;
                }
                else
                {
                    collectionModel.IsFollowing = false;
                }

                // Always true since these are saved collections
                collectionModel.IsSaved = true;

                // Check if caller liked the collection
                var likedCollection = await _unitOfWork.LikeCollectionRepository.GetByUserAndCollection(userId, collectionModel.Id);
                collectionModel.IsLiked = likedCollection != null && !likedCollection.IsDeleted;

                collectionModels.Add(collectionModel);
            }

            var paginatedResult = new Pagination<CollectionModel>(
                collectionModels,
                savedCollections.TotalCount,
                savedCollections.CurrentPage,
                savedCollections.PageSize
            );

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_SAVED_COLLECTIONS_SUCCESS,
                Data = new ModelPaging
                {
                    Data = paginatedResult,
                    MetaData = new
                    {
                        paginatedResult.TotalCount,
                        paginatedResult.PageSize,
                        paginatedResult.CurrentPage,
                        paginatedResult.TotalPages,
                        paginatedResult.HasNext,
                        paginatedResult.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> CheckIfCollectionSavedAsync(long userId, long collectionId)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Validate collection exists
            var collection = await _unitOfWork.CollectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new NotFoundException(MessageConstants.COLLECTION_NOT_FOUND);
            }

            var saveExists = await _unitOfWork.SaveCollectionRepository.GetByUserAndCollection(userId, collectionId);
            bool isSaved = saveExists != null && !saveExists.IsDeleted;

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.CHECK_SAVED_COLLECTION_SUCCESS,
                Data = new { IsSaved = isSaved }
            };
        }
    }
}
