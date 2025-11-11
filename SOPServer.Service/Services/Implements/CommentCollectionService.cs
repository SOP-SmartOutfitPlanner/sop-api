using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.CommentCollectionModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.Service.Services.Implements
{
    public class CommentCollectionService : ICommentCollectionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CommentCollectionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> CreateCommentCollection(CreateCommentCollectionModel model)
        {
            // Verify collection exists
            var collection = await _unitOfWork.CollectionRepository.GetByIdAsync(model.CollectionId);
            if (collection == null)
            {
                throw new NotFoundException(MessageConstants.COLLECTION_NOT_FOUND);
            }

            // Verify user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(model.UserId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var commentCollection = _mapper.Map<CommentCollection>(model);
            await _unitOfWork.CommentCollectionRepository.AddAsync(commentCollection);
            await _unitOfWork.SaveAsync();

            // Retrieve comment with user info
            var createdComment = await _unitOfWork.CommentCollectionRepository.GetByIdIncludeAsync(
                commentCollection.Id,
                include: query => query.Include(c => c.User)
            );

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.COMMENT_CREATE_SUCCESS,
                Data = _mapper.Map<CommentCollectionModel>(createdComment)
            };
        }

        public async Task<BaseResponseModel> UpdateCommentCollection(long id, UpdateCommentCollectionModel model)
        {
            var commentCollection = await _unitOfWork.CommentCollectionRepository.GetByIdIncludeAsync(
                id,
                include: query => query.Include(c => c.User)
            );

            if (commentCollection == null)
            {
                throw new NotFoundException(MessageConstants.COMMENT_NOT_FOUND);
            }

            commentCollection.Comment = model.Comment;
            commentCollection.UpdatedDate = DateTime.UtcNow;

            _unitOfWork.CommentCollectionRepository.UpdateAsync(commentCollection);
            await _unitOfWork.SaveAsync();

            var updatedComment = await _unitOfWork.CommentCollectionRepository.GetByIdIncludeAsync(
                id,
                include: query => query.Include(c => c.User)
            );

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.COMMENT_UPDATE_SUCCESS,
                Data = _mapper.Map<CommentCollectionModel>(updatedComment)
            };
        }

        public async Task<BaseResponseModel> DeleteCommentCollection(long id)
        {
            var commentCollection = await _unitOfWork.CommentCollectionRepository.GetByIdAsync(id);
            if (commentCollection == null)
            {
                throw new NotFoundException(MessageConstants.COMMENT_NOT_FOUND);
            }

            _unitOfWork.CommentCollectionRepository.SoftDeleteAsync(commentCollection);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.COMMENT_DELETE_SUCCESS
            };
        }

        public async Task<BaseResponseModel> GetCommentsByCollectionId(PaginationParameter paginationParameter, long collectionId)
        {
            // Verify collection exists
            var collection = await _unitOfWork.CollectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new NotFoundException(MessageConstants.COLLECTION_NOT_FOUND);
            }

            var comments = await _unitOfWork.CommentCollectionRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query.Include(c => c.User),
                filter: c => c.CollectionId == collectionId,
                orderBy: q => q.OrderByDescending(c => c.CreatedDate)
            );

            var commentModels = _mapper.Map<Pagination<CommentCollectionModel>>(comments);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_COMMENT_SUCCESS,
                Data = new ModelPaging
                {
                    Data = commentModels,
                    MetaData = new
                    {
                        commentModels.TotalCount,
                        commentModels.PageSize,
                        commentModels.CurrentPage,
                        commentModels.TotalPages,
                        commentModels.HasNext,
                        commentModels.HasPrevious
                    }
                }
            };
        }
    }
}
