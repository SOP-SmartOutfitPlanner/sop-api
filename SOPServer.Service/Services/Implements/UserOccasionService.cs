using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserOccasionModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.Service.Services.Implements
{
    public class UserOccasionService : IUserOccasionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserOccasionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> GetUserOccasionPaginationAsync(
            PaginationParameter paginationParameter,
            long userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? year = null,
            int? month = null,
            int? upcomingDays = null,
            bool? today = null)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Calculate date range based on parameters
            DateTime? filterStartDate = null;
            DateTime? filterEndDate = null;

            if (today.HasValue && today.Value)
            {
                // Get today's events only
                var todayDate = DateTime.Today;
                filterStartDate = todayDate;
                filterEndDate = todayDate.AddDays(1).AddTicks(-1);
            }
            else if (upcomingDays.HasValue && upcomingDays.Value > 0)
            {
                // Get events for next N days
                filterStartDate = DateTime.Today;
                filterEndDate = DateTime.Today.AddDays(upcomingDays.Value).AddTicks(-1);
            }
            else if (year.HasValue && month.HasValue)
            {
                // Get events for specific month and year
                if (month.Value < 1 || month.Value > 12)
                {
                    throw new BadRequestException("Month must be between 1 and 12");
                }
                filterStartDate = new DateTime(year.Value, month.Value, 1);
                filterEndDate = filterStartDate.Value.AddMonths(1).AddTicks(-1);
            }
            else if (year.HasValue)
            {
                // Get events for entire year
                filterStartDate = new DateTime(year.Value, 1, 1);
                filterEndDate = new DateTime(year.Value, 12, 31, 23, 59, 59);
            }
            else if (startDate.HasValue || endDate.HasValue)
            {
                // Use provided date range
                filterStartDate = startDate;
                filterEndDate = endDate;
            }

            var userOccasions = await _unitOfWork.UserOccasionRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(x => x.User)
                    .Include(x => x.Occasion),
                filter: x => x.UserId == userId &&
                           (string.IsNullOrWhiteSpace(paginationParameter.Search) ||
                            (x.Name != null && x.Name.Contains(paginationParameter.Search)) ||
                            (x.Description != null && x.Description.Contains(paginationParameter.Search))) &&
                           (!filterStartDate.HasValue || x.DateOccasion >= filterStartDate.Value) &&
                           (!filterEndDate.HasValue || x.DateOccasion <= filterEndDate.Value),
                orderBy: q => q.OrderByDescending(x => x.DateOccasion));

            var models = _mapper.Map<Pagination<UserOccasionModel>>(userOccasions);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_USER_OCCASION_SUCCESS,
                Data = new ModelPaging
                {
                    Data = models,
                    MetaData = new
                    {
                        models.TotalCount,
                        models.PageSize,
                        models.CurrentPage,
                        models.TotalPages,
                        models.HasNext,
                        models.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetUserOccasionByIdAsync(long id, long userId)
        {
            var userOccasion = await _unitOfWork.UserOccasionRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(x => x.User)
                    .Include(x => x.Occasion)
                    .Include(x => x.OutfitUsageHistories)
                        .ThenInclude(ouh => ouh.Outfit)
                            .ThenInclude(o => o.OutfitItems)
                                .ThenInclude(oi => oi.Item)
                                    .ThenInclude(i => i.Category));

            if (userOccasion == null)
            {
                throw new NotFoundException(MessageConstants.USER_OCCASION_NOT_FOUND);
            }

            if (userOccasion.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.USER_OCCASION_ACCESS_DENIED);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_OCCASION_GET_SUCCESS,
                Data = _mapper.Map<UserOccasionDetailedModel>(userOccasion)
            };
        }

        public async Task<BaseResponseModel> CreateUserOccasionAsync(long userId, UserOccasionCreateModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (model.OccasionId.HasValue)
            {
                var occasion = await _unitOfWork.OccasionRepository.GetByIdAsync(model.OccasionId.Value);
                if (occasion == null)
                {
                    throw new NotFoundException(MessageConstants.OCCASION_NOT_EXIST);
                }
            }

            var userOccasion = _mapper.Map<UserOccasion>(model);
            userOccasion.UserId = userId;

            await _unitOfWork.UserOccasionRepository.AddAsync(userOccasion);
            await _unitOfWork.SaveAsync();

            var created = await _unitOfWork.UserOccasionRepository.GetByIdIncludeAsync(
                userOccasion.Id,
                include: query => query
                    .Include(x => x.User)
                    .Include(x => x.Occasion));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.USER_OCCASION_CREATE_SUCCESS,
                Data = _mapper.Map<UserOccasionModel>(created)
            };
        }

        public async Task<BaseResponseModel> UpdateUserOccasionAsync(long id, long userId, UserOccasionUpdateModel model)
        {
            var userOccasion = await _unitOfWork.UserOccasionRepository.GetByIdAsync(id);
            if (userOccasion == null)
            {
                throw new NotFoundException(MessageConstants.USER_OCCASION_NOT_FOUND);
            }

            if (userOccasion.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.USER_OCCASION_ACCESS_DENIED);
            }

            if (model.OccasionId.HasValue)
            {
                var occasion = await _unitOfWork.OccasionRepository.GetByIdAsync(model.OccasionId.Value);
                if (occasion == null)
                {
                    throw new NotFoundException(MessageConstants.OCCASION_NOT_EXIST);
                }
            }

            if (!string.IsNullOrEmpty(model.Name))
                userOccasion.Name = model.Name;

            if (model.Description != null)
                userOccasion.Description = model.Description;

            if (model.DateOccasion.HasValue)
                userOccasion.DateOccasion = model.DateOccasion.Value;

            if (model.StartTime.HasValue)
                userOccasion.StartTime = model.StartTime;

            if (model.EndTime.HasValue)
                userOccasion.EndTime = model.EndTime;

            if (model.WeatherSnapshot != null)
                userOccasion.WeatherSnapshot = model.WeatherSnapshot;

            if (model.OccasionId.HasValue)
                userOccasion.OccasionId = model.OccasionId;

            _unitOfWork.UserOccasionRepository.UpdateAsync(userOccasion);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.UserOccasionRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(x => x.User)
                    .Include(x => x.Occasion));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_OCCASION_UPDATE_SUCCESS,
                Data = _mapper.Map<UserOccasionModel>(updated)
            };
        }

        public async Task<BaseResponseModel> DeleteUserOccasionAsync(long id, long userId)
        {
            var userOccasion = await _unitOfWork.UserOccasionRepository.GetByIdAsync(id);
            if (userOccasion == null)
            {
                throw new NotFoundException(MessageConstants.USER_OCCASION_NOT_FOUND);
            }

            // Check if the user occasion belongs to the current user
            if (userOccasion.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.USER_OCCASION_ACCESS_DENIED);
            }

            _unitOfWork.UserOccasionRepository.SoftDeleteAsync(userOccasion);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_OCCASION_DELETE_SUCCESS
            };
        }
    }
}
