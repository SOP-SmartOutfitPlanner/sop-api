using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.CommentPostModels;
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

        public CommentPostService(IUnitOfWork unitOfWork, IMapper mapper, ContentVisibilityHelper visibilityHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _visibilityHelper = visibilityHelper;
        }

        public async Task<BaseResponseModel> CreateNewComment(CreateCommentPostModel model)
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
            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.COMMENT_CREATE_SUCCESS,
                Data = _mapper.Map<CommentPostModel>(commentPost)
            };
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
