using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.LikeCollectionModels;
using SOPServer.Service.BusinessModels.NotificationModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.Service.Services.Implements
{
    public class LikeCollectionService : ILikeCollectionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly ILogger<LikeCollectionService> _logger;

        public LikeCollectionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            INotificationService notificationService,
            ILogger<LikeCollectionService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
            _logger = logger;
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

            // Send notification only when liking (not unliking)
            if (!likeCollection.IsDeleted)
            {
                await NotifyCollectionOwnerAboutLikeAsync(model.CollectionId, model.UserId);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = message,
                Data = _mapper.Map<LikeCollectionModel>(likeCollection)
            };
        }

        private async Task NotifyCollectionOwnerAboutLikeAsync(long collectionId, long likerId)
        {
            try
            {
                var collection = await _unitOfWork.CollectionRepository.GetByIdIncludeAsync(
                    collectionId,
                    include: query => query.Include(c => c.User));

                if (collection?.UserId == null || collection.UserId == likerId)
                {
                    // Don't notify if no collection owner or if liker is the owner
                    return;
                }

                var liker = await _unitOfWork.UserRepository.GetByIdAsync(likerId);
                var likerName = liker?.DisplayName ?? "Someone";
                var truncatedCollectionTitle = TruncateText(collection.Title, 30);

                var title = string.IsNullOrWhiteSpace(truncatedCollectionTitle)
                    ? "New like on your collection"
                    : $"New like on collection: {truncatedCollectionTitle}";

                var message = $"<b>{likerName}</b> liked your collection";

                var notificationModel = new NotificationRequestModel
                {
                    Title = title,
                    Message = message,
                    Href = $"/collections/{collectionId}",
                    ImageUrl = liker?.AvtUrl,
                    ActorUserId = likerId
                };

                await _notificationService.PushNotificationByUserId(collection.UserId.Value, notificationModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send like notification for collection {CollectionId}", collectionId);
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
    }
}
