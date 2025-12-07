using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.LikePostModels;
using SOPServer.Service.BusinessModels.NotificationModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.Service.Services.Implements
{
    public class LikePostService : ILikePostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly ILogger<LikePostService> _logger;

        public LikePostService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            INotificationService notificationService,
            ILogger<LikePostService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<BaseResponseModel> CreateLikePost(CreateLikePostModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(model.UserId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }
            var post = await _unitOfWork.PostRepository.GetByIdAsync(model.PostId);
            if (post == null)
            {
                throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
            }

            var likeExists = await _unitOfWork.LikePostRepository.GetByUserAndPost(model.UserId, model.PostId);

            LikePost likePost;
            string message;

            if (likeExists != null)
            {
                // Toggle like status
                likeExists.IsDeleted = !likeExists.IsDeleted;
                _unitOfWork.LikePostRepository.UpdateAsync(likeExists);
                likePost = likeExists;
                message = likeExists.IsDeleted ? MessageConstants.UNLIKE_POST_SUCCESS : MessageConstants.LIKE_POST_SUCCESS;
            }
            else
            {
                // Create new like
                likePost = _mapper.Map<LikePost>(model);
                await _unitOfWork.LikePostRepository.AddAsync(likePost);
                message = MessageConstants.LIKE_POST_SUCCESS;
            }

            await _unitOfWork.SaveAsync();

            // Send notification only when liking (not unliking)
            if (!likePost.IsDeleted)
            {
                await NotifyPostOwnerAboutLikeAsync(model.PostId, model.UserId);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = message,
                Data = _mapper.Map<LikePostModel>(likePost)
            };
        }

        private async Task NotifyPostOwnerAboutLikeAsync(long postId, long likerId)
        {
            try
            {
                var post = await _unitOfWork.PostRepository.GetByIdIncludeAsync(
                    postId,
                    include: query => query.Include(p => p.User));

                if (post?.UserId == null || post.UserId == likerId)
                {
                    // Don't notify if no post owner or if liker is the post owner
                    return;
                }

                var liker = await _unitOfWork.UserRepository.GetByIdAsync(likerId);
                var likerName = liker?.DisplayName ?? "Someone";
                var truncatedPostContent = TruncateText(post.Body, 30);

                var title = string.IsNullOrWhiteSpace(truncatedPostContent)
                    ? "New like on your post"
                    : $"New like on post: {truncatedPostContent}";

                var message = $"<b>{likerName}</b> liked your post";

                var notificationModel = new NotificationRequestModel
                {
                    Title = title,
                    Message = message,
                    Href = $"/posts/{postId}",
                    ImageUrl = liker?.AvtUrl,
                    ActorUserId = likerId
                };

                await _notificationService.PushNotificationByUserId(post.UserId.Value, notificationModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send like notification for post {PostId}", postId);
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
