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

namespace SOPServer.Service.Services.Implements
{
    public class CommentPostService : ICommentPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CommentPostService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

        public async Task<BaseResponseModel> GetCommentByParentId(PaginationParameter paginationParameter, long id)
        {
            // Get comments with the specified parent ID, including user and parent comment information
            var comments = await _unitOfWork.CommentPostRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(c => c.User)
                    .Include(c => c.ParentComment),
                filter: c => c.ParentCommentId == id,
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

        public async Task<BaseResponseModel> GetCommentParentByPostId(PaginationParameter paginationParameter, long postId)
        {
            var postExisted = await _unitOfWork.PostRepository.GetByIdAsync(postId);

            if (postExisted == null)
            {
                throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
            }

            var comments = await _unitOfWork.CommentPostRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(c => c.User),
                filter: c => c.PostId == postId && c.ParentCommentId == null,
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
