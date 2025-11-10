using AutoMapper;
using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ReportCommunityModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.Service.Services.Implements
{
    public class ReportCommunityService : IReportCommunityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReportCommunityService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> CreateReportAsync(ReportCommunityCreateModel model)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(model.UserId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Validate that at least PostId or CommentId is provided based on Type
            if (model.Type == ReportType.POST)
            {
                if (!model.PostId.HasValue)
                {
                    throw new BadRequestException(MessageConstants.REPORT_POST_ID_REQUIRED);
                }

                // Validate post exists
                var post = await _unitOfWork.PostRepository.GetByIdAsync(model.PostId.Value);
                if (post == null)
                {
                    throw new NotFoundException(MessageConstants.POST_NOT_FOUND);
                }
            }
            else if (model.Type == ReportType.COMMENT)
            {
                if (!model.CommentId.HasValue)
                {
                    throw new BadRequestException(MessageConstants.REPORT_COMMENT_ID_REQUIRED);
                }

                // Validate comment exists
                var comment = await _unitOfWork.CommentPostRepository.GetByIdAsync(model.CommentId.Value);
                if (comment == null)
                {
                    throw new NotFoundException(MessageConstants.COMMENT_NOT_FOUND);
                }
            }

            // Check for duplicate report
            var existingReport = await _unitOfWork.ReportCommunityRepository.GetExistingReportAsync(
                model.UserId, 
                model.PostId, 
                model.CommentId, 
                model.Type);

            if (existingReport != null)
            {
                throw new BadRequestException(MessageConstants.REPORT_ALREADY_EXISTS);
            }

            // Create report entity
            var report = _mapper.Map<ReportCommunity>(model);
            report.Status = ReportStatus.PENDING;
            report.Action = ReportAction.NONE;

            await _unitOfWork.ReportCommunityRepository.AddAsync(report);
            await _unitOfWork.SaveAsync();

            var reportModel = _mapper.Map<ReportCommunityModel>(report);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.REPORT_COMMUNITY_CREATE_SUCCESS,
                Data = reportModel
            };
        }
    }
}
