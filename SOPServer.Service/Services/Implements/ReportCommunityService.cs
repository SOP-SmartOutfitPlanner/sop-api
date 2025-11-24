using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.NotificationModels;
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

            // Check for existing report on the same content
            var existingReport = await _unitOfWork.ReportCommunityRepository.GetExistingReportByContentAsync(
                model.PostId,
                model.CommentId,
                model.Type);

            if (existingReport != null)
            {
                // Check if this user has already reported this content
                var existingReporter = await _unitOfWork.ReportReporterRepository
                    .GetByReportAndUserAsync(existingReport.Id, model.UserId);

                if (existingReporter != null)
                {
                    throw new BadRequestException(MessageConstants.REPORT_ALREADY_EXISTS);
                }

                // Merge this user as an additional reporter
                var reportReporter = new ReportReporter
                {
                    ReportId = existingReport.Id,
                    UserId = model.UserId,
                    Description = model.Description ?? string.Empty
                };

                await _unitOfWork.ReportReporterRepository.AddAsync(reportReporter);
                await _unitOfWork.SaveAsync();

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status201Created,
                    Message = MessageConstants.REPORT_COMMUNITY_MERGED_SUCCESS,
                    Data = new { ReportId = existingReport.Id, Merged = true }
                };
            }

            // Create new report entity
            var report = new ReportCommunity
            {
                PostId = model.PostId,
                CommentId = model.CommentId,
                Type = model.Type,
                Status = ReportStatus.PENDING,
                Action = ReportAction.NONE
            };

            await _unitOfWork.ReportCommunityRepository.AddAsync(report);
            await _unitOfWork.SaveAsync();

            // Add the first reporter
            var firstReporter = new ReportReporter
            {
                ReportId = report.Id,
                UserId = model.UserId,
                Description = model.Description ?? string.Empty
            };

            await _unitOfWork.ReportReporterRepository.AddAsync(firstReporter);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.REPORT_COMMUNITY_CREATE_SUCCESS,
                Data = new { ReportId = report.Id, Merged = false }
            };
        }

        public async Task<BaseResponseModel> GetPendingReportsAsync(ReportFilterModel filter, PaginationParameter pagination)
        {
            var (reports, totalCount) = await _unitOfWork.ReportCommunityRepository.GetPendingReportsAsync(
                filter.Type,
                filter.FromDate,
                filter.ToDate,
                pagination);

            var reportModels = reports.Select(r =>
            {
                var firstReporter = r.ReportReporters?.FirstOrDefault(rr => !rr.IsDeleted);
                return new ReportCommunityModel
                {
                    Id = r.Id,
                    OriginalReporter = firstReporter != null ? _mapper.Map<UserBasicModel>(firstReporter.User) : null,
                    Author = r.Type == ReportType.POST
                        ? _mapper.Map<UserBasicModel>(r.Post?.User)
                        : _mapper.Map<UserBasicModel>(r.CommentPost?.User),
                    PostId = r.PostId,
                    CommentId = r.CommentId,
                    Type = r.Type,
                    Action = r.Action,
                    Status = r.Status,
                    ReporterCount = r.ReportReporters?.Count(rr => !rr.IsDeleted) ?? 0,
                    CreatedDate = r.CreatedDate
                };
            }).ToList();

            var paginatedResult = new Pagination<ReportCommunityModel>(reportModels, totalCount, pagination.PageIndex, pagination.PageSize);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_PENDING_REPORTS_SUCCESS,
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

        public async Task<BaseResponseModel> GetAllReportsAsync(ReportFilterModel filter, PaginationParameter pagination)
        {
            var (reports, totalCount) = await _unitOfWork.ReportCommunityRepository.GetAllReportsAsync(
                filter.Type,
                filter.Status,
                filter.FromDate,
                filter.ToDate,
                pagination);

            var reportModels = reports.Select(r =>
            {
                var firstReporter = r.ReportReporters?.FirstOrDefault(rr => !rr.IsDeleted);
                return new ReportCommunityModel
                {
                    Id = r.Id,
                    OriginalReporter = firstReporter != null ? _mapper.Map<UserBasicModel>(firstReporter.User) : null,
                    Author = r.Type == ReportType.POST
                        ? _mapper.Map<UserBasicModel>(r.Post?.User)
                        : _mapper.Map<UserBasicModel>(r.CommentPost?.User),
                    PostId = r.PostId,
                    CommentId = r.CommentId,
                    Type = r.Type,
                    Action = r.Action,
                    Status = r.Status,
                    ReporterCount = r.ReportReporters?.Count(rr => !rr.IsDeleted) ?? 0,
                    CreatedDate = r.CreatedDate
                };
            }).ToList();

            var paginatedResult = new Pagination<ReportCommunityModel>(reportModels, totalCount, pagination.PageIndex, pagination.PageSize);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_ALL_REPORTS_SUCCESS,
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

            // Get original reporter (first reporter)
            var firstReporter = report.ReportReporters?.FirstOrDefault(rr => !rr.IsDeleted);

            var detailModel = new ReportDetailModel
            {
                Id = report.Id,
                Type = report.Type,
                Status = report.Status,
                Action = report.Action,
                CreatedDate = report.CreatedDate,
                OriginalReporter = firstReporter != null ? _mapper.Map<UserBasicModel>(firstReporter.User) : new UserBasicModel(),
                ReporterCount = report.ReportReporters?.Count(rr => !rr.IsDeleted) ?? 0,
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

        public async Task<BaseResponseModel> GetReportersByReportIdAsync(long reportId, PaginationParameter pagination)
        {
            // Validate report exists
            var report = await _unitOfWork.ReportCommunityRepository.GetByIdAsync(reportId);
            if (report == null)
            {
                throw new NotFoundException(MessageConstants.REPORT_NOT_FOUND);
            }

            var reporters = await _unitOfWork.ReportReporterRepository.ToPaginationIncludeAsync(
                pagination,
                include: query => query.Include(rr => rr.User),
                filter: rr => rr.ReportId == reportId,
                orderBy: q => q.OrderByDescending(rr => rr.CreatedDate));

            var reporterModels = reporters.Select(rr => new ReporterModel
            {
                Id = rr.Id,
                UserId = rr.UserId,
                Reporter = _mapper.Map<UserBasicModel>(rr.User),
                Description = rr.Description,
                CreatedDate = rr.CreatedDate
            }).ToList();

            var paginatedResult = new Pagination<ReporterModel>(
                reporterModels,
                reporters.TotalCount,
                reporters.CurrentPage,
                reporters.PageSize);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_REPORTERS_SUCCESS,
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

        public async Task<BaseResponseModel> ResolveNoViolationAsync(long reportId, long adminId, ResolveNoViolationModel model)
        {
            var report = await _unitOfWork.ReportCommunityRepository.GetReportDetailsAsync(reportId);
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

            // Notify all reporters
            var reporterIds = report.ReportReporters?
                .Where(rr => !rr.IsDeleted)
                .Select(rr => rr.UserId)
                .Distinct()
                .ToList() ?? new List<long>();

            foreach (var reporterId in reporterIds)
            {
                await NotifyReporterAsync(reporterId, report, "No violation found");
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.RESOLVE_REPORT_NO_VIOLATION_SUCCESS,
                Data = new { ReportId = reportId }
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

            // Notify all reporters
            var reporterIds = report.ReportReporters?
                .Where(rr => !rr.IsDeleted)
                .Select(rr => rr.UserId)
                .Distinct()
                .ToList() ?? new List<long>();

            foreach (var reporterId in reporterIds)
            {
                await NotifyReporterAsync(reporterId, report, $"Action taken: {model.Action}");
            }

            // Notify author
            await NotifyAuthorAsync(authorId, model.Action, model.Notes);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.RESOLVE_REPORT_WITH_ACTION_SUCCESS,
                Data = new { ReportId = reportId }
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

        private async Task NotifyReporterAsync(long reporterId, ReportCommunity report, string outcome)
        {
            // Get author display name
            var authorName = "Unknown";
            if (report.Type == ReportType.POST && report.Post?.User != null)
            {
                authorName = report.Post.User.DisplayName;
            }
            else if (report.Type == ReportType.COMMENT && report.CommentPost?.User != null)
            {
                authorName = report.CommentPost.User.DisplayName;
            }

            // Format content type to Pascal case (Post/Comment)
            var contentType = report.Type == ReportType.POST ? "Post" : "Comment";

            var notificationModel = new NotificationRequestModel
            {
                Title = "Report Update",
                Message = $"Your report about {authorName}'s {contentType} has been reviewed. Outcome: {outcome}",
                ActorUserId = reporterId
            };

            try
            {
                await _notificationService.PushNotificationByUserId(reporterId, notificationModel);
            }
            catch
            {
                // Log error but continue processing
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

            var notificationModel = new NotificationRequestModel
            {
                Title = "Content Moderation Notice",
                Message = $"{actionMessages[action]} Reason: {reason}",
                ActorUserId = authorId
            };

            try
            {
                await _notificationService.PushNotificationByUserId(authorId, notificationModel);
            }
            catch
            {
                // Log error but continue processing
            }
        }
    }
}
