using AutoMapper;
using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ReportCommunityModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.Utils;

namespace SOPServer.Service.Services.Implements
{
    public class ReportCommunityService : IReportCommunityService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public ReportCommunityService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
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

        public async Task<BaseResponseModel> GetPendingReportsAsync(ReportFilterModel filter, PaginationParameter pagination)
        {
            var (reports, totalCount) = await _unitOfWork.ReportCommunityRepository.GetPendingReportsAsync(
                filter.Type,
                filter.FromDate,
                filter.ToDate,
                pagination);

            var reportModels = reports.Select(r => new ReportCommunityModel
            {
                Id = r.Id,
                UserId = r.UserId,
                PostId = r.PostId,
                CommentId = r.CommentId,
                Type = r.Type,
                Action = r.Action,
                Status = r.Status,
                Description = r.Description,
                CreatedDate = r.CreatedDate
            }).ToList();

            var paginatedResult = new Pagination<ReportCommunityModel>(reportModels, totalCount, pagination.PageIndex, pagination.PageSize);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_PENDING_REPORTS_SUCCESS,
                Data = paginatedResult
            };
        }

        public async Task<BaseResponseModel> GetReportDetailsAsync(long reportId)
        {
            var report = await _unitOfWork.ReportCommunityRepository.GetReportDetailsAsync(reportId);
            if (report == null)
            {
                throw new NotFoundException(MessageConstants.REPORT_NOT_FOUND);
            }

            // Get author ID based on content type
            long authorId = 0;
            if (report.Type == ReportType.POST && report.Post != null)
            {
                authorId = report.Post.UserId ?? 0;
            }
            else if (report.Type == ReportType.COMMENT && report.CommentPost != null)
            {
                authorId = report.CommentPost.UserId;
            }

            // Get author violation history
            var warningCount = 0;
            var suspensionCount = 0;
            if (authorId > 0)
            {
                var violations = await _unitOfWork.UserViolationRepository.GetViolationHistoryAsync(authorId);
                warningCount = violations.Count(v => v.ViolationType == "WARN");
                suspensionCount = violations.Count(v => v.ViolationType == "SUSPEND");
            }

            // Determine hidden status for admin visibility
            string? hiddenStatus = null;
            if (report.Type == ReportType.POST && report.Post != null && report.Post.IsHidden)
            {
                hiddenStatus = $"Hidden by moderation on {report.Post.UpdatedDate?.ToString("yyyy-MM-dd") ?? report.ResolvedAt?.ToString("yyyy-MM-dd") ?? "N/A"}";
            }
            else if (report.Type == ReportType.COMMENT && report.CommentPost != null && report.CommentPost.IsHidden)
            {
                hiddenStatus = $"Hidden by moderation on {report.CommentPost.UpdatedDate?.ToString("yyyy-MM-dd") ?? report.ResolvedAt?.ToString("yyyy-MM-dd") ?? "N/A"}";
            }

            var detailModel = new ReportDetailModel
            {
                Id = report.Id,
                Type = report.Type,
                Status = report.Status,
                Action = report.Action,
                Description = report.Description,
                CreatedDate = report.CreatedDate,
                Reporter = _mapper.Map<UserBasicModel>(report.User),
                Content = report.Type == ReportType.POST
                    ? _mapper.Map<ReportedContentModel>(report.Post)
                    : _mapper.Map<ReportedContentModel>(report.CommentPost),
                Author = report.Type == ReportType.POST
                    ? _mapper.Map<UserBasicModel>(report.Post?.User)
                    : _mapper.Map<UserBasicModel>(report.CommentPost?.User),
                ResolvedByAdminId = report.ResolvedByAdminId,
                ResolvedAt = report.ResolvedAt,
                ResolutionNotes = report.ResolutionNotes,
                HiddenStatus = hiddenStatus,
                AuthorWarningCount = warningCount,
                AuthorSuspensionCount = suspensionCount
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_REPORT_DETAILS_SUCCESS,
                Data = detailModel
            };
        }

        public async Task<BaseResponseModel> ResolveNoViolationAsync(long reportId, long adminId, ResolveNoViolationModel model)
        {
            var report = await _unitOfWork.ReportCommunityRepository.GetByIdAsync(reportId);
            if (report == null)
            {
                throw new NotFoundException(MessageConstants.REPORT_NOT_FOUND);
            }

            if (report.Status != ReportStatus.PENDING)
            {
                throw new BadRequestException(MessageConstants.REPORT_ALREADY_RESOLVED);
            }

            // Update report
            report.Status = ReportStatus.RESOLVED;
            report.Action = ReportAction.NONE;
            report.ResolvedByAdminId = adminId;
            report.ResolvedAt = DateTime.UtcNow;
            report.ResolutionNotes = model.Notes ?? "No violation found";

            _unitOfWork.ReportCommunityRepository.UpdateAsync(report);
            await _unitOfWork.SaveAsync();

            // Notify reporter
            await NotifyReporterAsync(report.UserId, report.Type.ToString(), "No violation found");

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.RESOLVE_REPORT_NO_VIOLATION_SUCCESS,
                Data = _mapper.Map<ReportCommunityModel>(report)
            };
        }

        public async Task<BaseResponseModel> ResolveWithActionAsync(long reportId, long adminId, ResolveWithActionModel model)
        {
            // Validate action
            if (model.Action == ReportAction.NONE)
            {
                throw new BadRequestException(MessageConstants.INVALID_REPORT_ACTION);
            }

            if (model.Action == ReportAction.SUSPEND && (!model.SuspensionDays.HasValue || model.SuspensionDays.Value <= 0))
            {
                throw new BadRequestException(MessageConstants.SUSPENSION_DAYS_REQUIRED);
            }

            var report = await _unitOfWork.ReportCommunityRepository.GetReportDetailsAsync(reportId);
            if (report == null)
            {
                throw new NotFoundException(MessageConstants.REPORT_NOT_FOUND);
            }

            if (report.Status != ReportStatus.PENDING)
            {
                throw new BadRequestException(MessageConstants.REPORT_ALREADY_RESOLVED);
            }

            // Get author ID
            long authorId = 0;
            long contentId = 0;
            if (report.Type == ReportType.POST && report.Post != null)
            {
                authorId = report.Post.UserId ?? 0;
                contentId = report.Post.Id;
            }
            else if (report.Type == ReportType.COMMENT && report.CommentPost != null)
            {
                authorId = report.CommentPost.UserId;
                contentId = report.CommentPost.Id;
            }

            if (authorId == 0 || contentId == 0)
            {
                throw new NotFoundException(MessageConstants.CONTENT_NOT_FOUND_FOR_ACTION);
            }

            // Apply action
            switch (model.Action)
            {
                case ReportAction.HIDE:
                    await ApplyHideActionAsync(contentId, report.Type);
                    break;
                case ReportAction.DELETE:
                    await ApplyDeleteActionAsync(contentId, report.Type);
                    break;
                case ReportAction.WARN:
                    await ApplyWarnActionAsync(authorId, reportId, model.Notes);
                    break;
                case ReportAction.SUSPEND:
                    await ApplySuspendActionAsync(authorId, reportId, model.SuspensionDays!.Value, model.Notes, adminId);
                    break;
            }

            // Update report
            report.Status = ReportStatus.RESOLVED;
            report.Action = model.Action;
            report.ResolvedByAdminId = adminId;
            report.ResolvedAt = DateTime.UtcNow;
            report.ResolutionNotes = model.Notes;

            _unitOfWork.ReportCommunityRepository.UpdateAsync(report);
            await _unitOfWork.SaveAsync();

            // Notify both reporter and author
            await NotifyReporterAsync(report.UserId, report.Type.ToString(), $"Action taken: {model.Action}");
            await NotifyAuthorAsync(authorId, model.Action, model.Notes);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.RESOLVE_REPORT_WITH_ACTION_SUCCESS,
                Data = _mapper.Map<ReportCommunityModel>(report)
            };
        }

        // Helper methods
        private async Task ApplyHideActionAsync(long contentId, ReportType type)
        {
            if (type == ReportType.POST)
            {
                var post = await _unitOfWork.PostRepository.GetByIdAsync(contentId);
                if (post != null)
                {
                    post.IsHidden = true;
                    _unitOfWork.PostRepository.UpdateAsync(post);
                }
            }
            else
            {
                var comment = await _unitOfWork.CommentPostRepository.GetByIdAsync(contentId);
                if (comment != null)
                {
                    comment.IsHidden = true;
                    _unitOfWork.CommentPostRepository.UpdateAsync(comment);
                }
            }
        }

        private async Task ApplyDeleteActionAsync(long contentId, ReportType type)
        {
            if (type == ReportType.POST)
            {
                var post = await _unitOfWork.PostRepository.GetByIdAsync(contentId);
                if (post != null)
                {
                    post.IsDeleted = true;
                    _unitOfWork.PostRepository.UpdateAsync(post);
                }
            }
            else
            {
                var comment = await _unitOfWork.CommentPostRepository.GetByIdAsync(contentId);
                if (comment != null)
                {
                    comment.IsDeleted = true;
                    _unitOfWork.CommentPostRepository.UpdateAsync(comment);
                }
            }
        }

        private async Task ApplyWarnActionAsync(long userId, long reportId, string notes)
        {
            var violation = new UserViolation
            {
                UserId = userId,
                ViolationType = "WARN",
                OccurredAt = DateTime.UtcNow,
                ReportId = reportId,
                Notes = notes
            };
            await _unitOfWork.UserViolationRepository.AddAsync(violation);
        }

        private async Task ApplySuspendActionAsync(long userId, long reportId, int days, string reason, long adminId)
        {
            var suspension = new UserSuspension
            {
                UserId = userId,
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddDays(days),
                Reason = reason,
                ReportId = reportId,
                CreatedByAdminId = adminId,
                IsActive = true
            };
            await _unitOfWork.UserSuspensionRepository.AddAsync(suspension);

            // Also create violation record
            var violation = new UserViolation
            {
                UserId = userId,
                ViolationType = "SUSPEND",
                OccurredAt = DateTime.UtcNow,
                ReportId = reportId,
                Notes = $"Suspended for {days} days: {reason}"
            };
            await _unitOfWork.UserViolationRepository.AddAsync(violation);
        }

        private async Task NotifyReporterAsync(long reporterId, string contentType, string outcome)
        {
            // For now, we'll skip creating formal notifications and just send push notifications
            // Future: Can integrate with NotificationService to create persisted notifications
            var devices = await _unitOfWork.UserDeviceRepository.GetUserDeviceByUserId(reporterId);
            if (devices != null && devices.Any())
            {
                var title = "Report Update";
                var body = $"Your report about {contentType} has been reviewed. Outcome: {outcome}";

                foreach (var device in devices)
                {
                    try
                    {
                        await FirebaseLibrary.SendMessageFireBase(title, body, device.DeviceToken);
                    }
                    catch
                    {
                        // Continue to next device if one fails
                    }
                }
            }
        }

        private async Task NotifyAuthorAsync(long authorId, ReportAction action, string reason)
        {
            var actionMessages = new Dictionary<ReportAction, string>
            {
                { ReportAction.HIDE, "Your content has been hidden for violating community guidelines." },
                { ReportAction.DELETE, "Your content has been removed for violating community guidelines." },
                { ReportAction.WARN, "You have received a warning for violating community guidelines." },
                { ReportAction.SUSPEND, "Your account has been temporarily suspended for violating community guidelines." }
            };

            var devices = await _unitOfWork.UserDeviceRepository.GetUserDeviceByUserId(authorId);
            if (devices != null && devices.Any())
            {
                var title = "Content Moderation Notice";
                var body = $"{actionMessages[action]} Reason: {reason}";

                foreach (var device in devices)
                {
                    try
                    {
                        await FirebaseLibrary.SendMessageFireBase(title, body, device.DeviceToken);
                    }
                    catch
                    {
                        // Continue to next device if one fails
                    }
                }
            }
        }
    }
}
