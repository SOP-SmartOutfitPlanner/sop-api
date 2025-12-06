using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.CommentPostModels;
using SOPServer.Service.BusinessModels.NotificationModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.Utils;

namespace SOPServer.Service.Services.Implements
{
    public class CommentPostService : ICommentPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ContentVisibilityHelper _visibilityHelper;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CommentPostService> _logger;

        public CommentPostService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ContentVisibilityHelper visibilityHelper,
            INotificationService notificationService,
            ILogger<CommentPostService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _visibilityHelper = visibilityHelper;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<BaseResponseModel> CreateNewComment(CreateCommentPostModel model)
        {
            if (model.ParentCommentId != null)
            {
                var parentComment = await _unitOfWork.CommentPostRepository.GetByIdIncludeAsync((long)model.ParentCommentId, include: x => x.Include(q => q.ParentComment));
                if (parentComment == null)
                {
                    throw new NotFoundException(MessageConstants.PARENT_COMMENT_NOT_FOUND);
                }

                if (parentComment.ParentComment != null)
                {
                    throw new BadRequestException(MessageConstants.COMMENT_CANNOT_REPLY_MORE_THAN_ONE_LEVEL);
                }
            }

            // Check if user is suspended
            var suspension = await _unitOfWork.UserSuspensionRepository.GetActiveSuspensionAsync(model.UserId);
            if (suspension != null && suspension.EndAt > DateTime.UtcNow)
            {
                throw new ForbiddenException(
                    $"Your account is suspended until {suspension.EndAt:yyyy-MM-dd HH:mm} UTC. " +
                    $"Reason: {suspension.Reason}. You cannot create comments during this period.");
            }

            var commentPost = _mapper.Map<CommentPost>(model);
            await _unitOfWork.CommentPostRepository.AddAsync(commentPost);
            await _unitOfWork.SaveAsync();

            // Send notification to post owner
            await NotifyPostOwnerAboutCommentAsync(model.PostId, model.UserId, model.Comment);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.COMMENT_CREATE_SUCCESS,
                Data = _mapper.Map<CommentPostModel>(commentPost)
            };
        }

        private async Task NotifyPostOwnerAboutCommentAsync(long postId, long commenterId, string? commentText)
        {
            try
            {
                var post = await _unitOfWork.PostRepository.GetByIdIncludeAsync(
                    postId,
                    include: query => query.Include(p => p.User));

                if (post?.UserId == null || post.UserId == commenterId)
                {
                    // Don't notify if no post owner or if commenter is the post owner
                    return;
                }

                var commenter = await _unitOfWork.UserRepository.GetByIdAsync(commenterId);
                var commenterName = commenter?.DisplayName ?? "Someone";
                var truncatedPostContent = TruncateText(post.Body, 30);
                var truncatedComment = TruncateText(commentText, 100);

                var title = string.IsNullOrWhiteSpace(truncatedPostContent)
                    ? "New comment on your post"
                    : $"New comment on post: {truncatedPostContent}";

                var message = string.IsNullOrWhiteSpace(truncatedComment)
                    ? $"<b>{commenterName}</b> commented on your post"
                    : $"<b>{commenterName}</b> commented: {truncatedComment}";

                var notificationModel = new NotificationRequestModel
                {
                    Title = title,
                    Message = message,
                    Href = $"/posts/{postId}",
                    ImageUrl = commenter?.AvtUrl,
                    ActorUserId = commenterId
                };

                await _notificationService.PushNotificationByUserId(post.UserId.Value, notificationModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send comment notification for post {PostId}", postId);
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

        public async Task<BaseResponseModel> UpdateCommentPost(long id, UpdateCommentPostModel model)
        {
            var commentPost = await _unitOfWork.CommentPostRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(c => c.User)
                    .Include(c => c.ParentComment)
            );

            if (commentPost == null)
            {
                throw new NotFoundException(MessageConstants.COMMENT_NOT_FOUND);
            }

            commentPost.Comment = model.Comment;
            commentPost.UpdatedDate = DateTime.UtcNow;

            _unitOfWork.CommentPostRepository.UpdateAsync(commentPost);
            await _unitOfWork.SaveAsync();

            var updatedComment = await _unitOfWork.CommentPostRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(c => c.User)
                    .Include(c => c.ParentComment)
            );

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.COMMENT_UPDATE_SUCCESS,
                Data = _mapper.Map<CommentPostModel>(updatedComment)
            };
        }

        public async Task<BaseResponseModel> DeleteCommentPost(int id)
        {
            var commentPost = await _unitOfWork.CommentPostRepository.GetByIdAsync(id);
            if (commentPost == null)
            {
                throw new NotFoundException(MessageConstants.COMMENT_NOT_FOUND);
            }
            _unitOfWork.CommentPostRepository.SoftDeleteAsync(commentPost);
            await _unitOfWork.SaveAsync();
            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.COMMENT_DELETE_SUCCESS,
            };
        }

        public async Task<BaseResponseModel> GetCommentByParentId(PaginationParameter paginationParameter, long id, long? requesterId = null)
        {
            // Validate parent comment exists and check if it's hidden
            var parentComment = await _unitOfWork.CommentPostRepository.GetByIdIncludeAsync(
                id,
                include: query => query.Include(c => c.Post));

            if (parentComment == null)
            {
                throw new NotFoundException(MessageConstants.COMMENT_NOT_FOUND);
            }

            // Check if parent comment is hidden
            if (parentComment.IsHidden)
            {
                bool isAdmin = requesterId.HasValue && await _visibilityHelper.IsUserAdminAsync(requesterId.Value);
                bool canView = ContentVisibilityHelper.CanViewHiddenContent(parentComment.UserId, requesterId, isAdmin);

                if (!canView)
                {
                    throw new NotFoundException(MessageConstants.COMMENTS_NOT_FOUND_POST_HIDDEN);
                }
            }

            // If parent comment belongs to a post, check post visibility
            if (parentComment.Post != null && parentComment.Post.IsHidden)
            {
                bool isAdmin = requesterId.HasValue && await _visibilityHelper.IsUserAdminAsync(requesterId.Value);
                bool canView = ContentVisibilityHelper.CanViewHiddenContent(parentComment.Post.UserId ?? 0, requesterId, isAdmin);

                if (!canView)
                {
                    throw new NotFoundException(MessageConstants.COMMENTS_NOT_FOUND_POST_HIDDEN);
                }
            }

            // Determine if requester can see hidden comments
            bool isRequesterAdmin = requesterId.HasValue && await _visibilityHelper.IsUserAdminAsync(requesterId.Value);

            // Get comments with the specified parent ID, including user and parent comment information
            var comments = await _unitOfWork.CommentPostRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(c => c.User)
                    .Include(c => c.ParentComment),
                filter: c => c.ParentCommentId == id && (!c.IsHidden || c.UserId == requesterId || isRequesterAdmin),
                orderBy: q => q.OrderByDescending(c => c.CreatedDate));

            var commentModels = _mapper.Map<Pagination<CommentPostModel>>(comments);

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

        public async Task<BaseResponseModel> GetCommentParentByPostId(PaginationParameter paginationParameter, long postId, long? requesterId = null)
        {
            var postExisted = await _unitOfWork.PostRepository.GetByIdAsync(postId);

            if (postExisted == null)
            {
                throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
            }

            // Check if post is hidden and user is authorized to view comments
            if (postExisted.IsHidden)
            {
                bool isAdmin = requesterId.HasValue && await _visibilityHelper.IsUserAdminAsync(requesterId.Value);
                bool canView = ContentVisibilityHelper.CanViewHiddenContent(postExisted.UserId ?? 0, requesterId, isAdmin);

                if (!canView)
                {
                    throw new NotFoundException(MessageConstants.COMMENTS_NOT_FOUND_POST_HIDDEN);
                }
            }

            // Determine if requester can see hidden comments
            bool isRequesterAdmin = requesterId.HasValue && await _visibilityHelper.IsUserAdminAsync(requesterId.Value);

            var comments = await _unitOfWork.CommentPostRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(c => c.User),
                filter: c => c.PostId == postId && c.ParentCommentId == null && (!c.IsHidden || c.UserId == requesterId || isRequesterAdmin),
                orderBy: q => q.OrderByDescending(c => c.CreatedDate));

            var commentModels = _mapper.Map<Pagination<CommentPostModel>>(comments);

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
