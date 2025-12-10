using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
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

            // Validate that the name is not "Daily" (case-insensitive)
            if (!string.IsNullOrWhiteSpace(model.Name) &&
                model.Name.Trim().Equals("Daily", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException(MessageConstants.USER_OCCASION_DAILY_NAME_RESERVED);
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

            // Validate that the name is not "Daily" (case-insensitive)
            if (!string.IsNullOrWhiteSpace(model.Name) &&
                model.Name.Trim().Equals("Daily", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException(MessageConstants.USER_OCCASION_DAILY_NAME_RESERVED);
            }

            if (model.OccasionId.HasValue)
            {
                var occasion = await _unitOfWork.OccasionRepository.GetByIdAsync(model.OccasionId.Value);
                if (occasion == null)
                {
                    throw new NotFoundException(MessageConstants.OCCASION_NOT_EXIST);
                }
            }

            // Check if date/time changes require usage history update
            bool dateTimeChanged = (model.StartTime.HasValue && model.StartTime != userOccasion.StartTime) ||
                                 (model.DateOccasion.HasValue && model.DateOccasion.Value != userOccasion.DateOccasion);

            DateTime? oldWornAtDateTime = null;
            DateTime? newWornAtDateTime = null;

            if (dateTimeChanged)
            {
                // Store old worn at time
                oldWornAtDateTime = userOccasion.StartTime ?? userOccasion.DateOccasion;

                // Calculate new worn at time
                var tempStartTime = model.StartTime ?? userOccasion.StartTime;
                var tempDateOccasion = model.DateOccasion ?? userOccasion.DateOccasion;
                newWornAtDateTime = tempStartTime ?? tempDateOccasion;

                // Get all outfit usage history records for this occasion
                var outfitUsageHistories = await _unitOfWork.OutfitUsageHistoryRepository.GetQueryable()
                    .Include(ouh => ouh.Outfit)
                        .ThenInclude(o => o.OutfitItems)
                    .Where(ouh => ouh.UserOccassionId == id && !ouh.IsDeleted)
                    .ToListAsync();

                // Collect all unique items from all outfits
                var allItemsToUpdate = new HashSet<long>();
                foreach (var usageHistory in outfitUsageHistories)
                {
                    if (usageHistory.Outfit?.OutfitItems != null)
                    {
                        foreach (var outfitItem in usageHistory.Outfit.OutfitItems.Where(oi => oi.ItemId.HasValue && !oi.IsDeleted))
                        {
                            allItemsToUpdate.Add(outfitItem.ItemId.Value);
                        }
                    }
                }

                // Update item usage tracking with new date
                if (allItemsToUpdate.Count > 0 && oldWornAtDateTime.HasValue && newWornAtDateTime.HasValue)
                {
                    await UpdateItemWornAtHistoryAsync(allItemsToUpdate, oldWornAtDateTime.Value, newWornAtDateTime.Value);
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

            // Get all outfit usage history records for this occasion
            var outfitUsageHistories = await _unitOfWork.OutfitUsageHistoryRepository.GetQueryable()
                .Include(ouh => ouh.Outfit)
                    .ThenInclude(o => o.OutfitItems)
                .Where(ouh => ouh.UserOccassionId == id && !ouh.IsDeleted)
                .ToListAsync();

            // Get the worn at time for usage tracking
            DateTime? wornAtDateTime = userOccasion.StartTime ?? userOccasion.DateOccasion;

            // Collect all unique items from all outfits
            var allItemsToUpdate = new HashSet<long>();
            foreach (var usageHistory in outfitUsageHistories)
            {
                if (usageHistory.Outfit?.OutfitItems != null)
                {
                    foreach (var outfitItem in usageHistory.Outfit.OutfitItems.Where(oi => oi.ItemId.HasValue && !oi.IsDeleted))
                    {
                        allItemsToUpdate.Add(outfitItem.ItemId.Value);
                    }
                }
            }

            // Check if we should run in background (threshold: more than 50 items)
            const int backgroundThreshold = 50;
            if (allItemsToUpdate.Count > backgroundThreshold)
            {
                // Fire and forget - run in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UpdateItemUsageTrackingAsync(allItemsToUpdate, wornAtDateTime.Value);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't throw (fire and forget)
                        Console.WriteLine($"[ERROR] Background item usage update failed: {ex.Message}");
                    }
                });
            }
            else
            {
                // Run synchronously for small number of items
                await UpdateItemUsageTrackingAsync(allItemsToUpdate, wornAtDateTime.Value);
            }

            _unitOfWork.UserOccasionRepository.SoftDeleteAsync(userOccasion);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_OCCASION_DELETE_SUCCESS
            };
        }

        private async Task UpdateItemUsageTrackingAsync(HashSet<long> itemIds, DateTime wornAtDateTime)
        {
            foreach (var itemId in itemIds)
            {
                var item = await _unitOfWork.ItemRepository.GetByIdAsync(itemId);
                if (item != null && item.ItemType != ItemType.SYSTEM)
                {
                    // Decrement usage count
                    if (item.UsageCount > 0)
                    {
                        item.UsageCount--;
                    }

                    // Find and remove the specific worn at history entry
                    var wornAtHistoryToRemove = await _unitOfWork.ItemWornAtHistoryRepository
                        .GetQueryable()
                        .FirstOrDefaultAsync(w => w.ItemId == item.Id && w.WornAt == wornAtDateTime && !w.IsDeleted);

                    if (wornAtHistoryToRemove != null)
                    {
                        _unitOfWork.ItemWornAtHistoryRepository.SoftDeleteAsync(wornAtHistoryToRemove);
                    }

                    // Update LastWornAt to the most recent date from history, or null if no history
                    var latestWornAt = await _unitOfWork.ItemWornAtHistoryRepository
                        .GetQueryable()
                        .Where(w => w.ItemId == item.Id && !w.IsDeleted)
                        .OrderByDescending(w => w.WornAt)
                        .Select(w => (DateTime?)w.WornAt)
                        .FirstOrDefaultAsync();

                    item.LastWornAt = latestWornAt;

                    _unitOfWork.ItemRepository.UpdateAsync(item);
                }
            }
            await _unitOfWork.SaveAsync();
        }

        private async Task UpdateItemWornAtHistoryAsync(HashSet<long> itemIds, DateTime oldWornAtDateTime, DateTime newWornAtDateTime)
        {
            foreach (var itemId in itemIds)
            {
                var item = await _unitOfWork.ItemRepository.GetByIdAsync(itemId);
                if (item != null && item.ItemType != ItemType.SYSTEM)
                {
                    // Find the worn at history entry with old date
                    var wornAtHistoryToUpdate = await _unitOfWork.ItemWornAtHistoryRepository
                        .GetQueryable()
                        .FirstOrDefaultAsync(w => w.ItemId == item.Id && w.WornAt == oldWornAtDateTime && !w.IsDeleted);

                    if (wornAtHistoryToUpdate != null)
                    {
                        // Update the worn at date
                        wornAtHistoryToUpdate.WornAt = newWornAtDateTime;
                        _unitOfWork.ItemWornAtHistoryRepository.UpdateAsync(wornAtHistoryToUpdate);
                    }

                    // Update LastWornAt to the most recent date from history
                    var latestWornAt = await _unitOfWork.ItemWornAtHistoryRepository
                        .GetQueryable()
                        .Where(w => w.ItemId == item.Id && !w.IsDeleted)
                        .OrderByDescending(w => w.WornAt)
                        .Select(w => (DateTime?)w.WornAt)
                        .FirstOrDefaultAsync();

                    item.LastWornAt = latestWornAt;

                    _unitOfWork.ItemRepository.UpdateAsync(item);
                }
            }
            await _unitOfWork.SaveAsync();
        }
    }
}
