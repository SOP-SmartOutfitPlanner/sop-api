using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.CommentCollectionModels;
using SOPServer.Service.BusinessModels.NotificationModels;
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
        private readonly INotificationService _notificationService;
        private readonly ILogger<CommentCollectionService> _logger;

        public CommentCollectionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            INotificationService notificationService,
            ILogger<CommentCollectionService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
            _logger = logger;
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

            // Send notification to collection owner
            await NotifyCollectionOwnerAboutCommentAsync(collection, model.UserId, model.Comment);

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

        private async Task NotifyCollectionOwnerAboutCommentAsync(Collection collection, long commenterId, string? commentText)
        {
            try
            {
                if (collection.UserId == null || collection.UserId == commenterId)
                {
                    // Don't notify if no collection owner or if commenter is the owner
                    return;
                }

                var commenter = await _unitOfWork.UserRepository.GetByIdAsync(commenterId);
                var commenterName = commenter?.DisplayName ?? "Someone";
                var truncatedCollectionTitle = TruncateText(collection.Title, 30);
                var truncatedComment = TruncateText(commentText, 100);

                var title = string.IsNullOrWhiteSpace(truncatedCollectionTitle)
                    ? "New comment on your collection"
                    : $"New comment on collection: {truncatedCollectionTitle}";

                var message = string.IsNullOrWhiteSpace(truncatedComment)
                    ? $"<b>{commenterName}</b> commented on your collection"
                    : $"<b>{commenterName}</b> commented: {truncatedComment}";

                var notificationModel = new NotificationRequestModel
                {
                    Title = title,
                    Message = message,
                    Href = $"/collections/{collection.Id}",
                    ImageUrl = commenter?.AvtUrl,
                    ActorUserId = commenterId
                };

                await _notificationService.PushNotificationByUserId(collection.UserId.Value, notificationModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send comment notification for collection {CollectionId}", collection.Id);
            }
        }

        private static string? TruncateText(string? text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var trimmed = text.Trim();
            if (trimmed.Length <= maxLength)
            {
                return trimmed;
            }

            return trimmed.Substring(0, maxLength) + "...";
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
