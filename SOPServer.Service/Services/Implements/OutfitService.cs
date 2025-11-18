using AutoMapper;
using GenerativeAI.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.OutfitCalendarModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.BusinessModels.QDrantModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserModels;
using SOPServer.Service.BusinessModels.UserOccasionModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SOPServer.Service.Services.Implements
{
    public class OutfitService : IOutfitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IGeminiService _geminiService;
        private readonly IQdrantService _qdrantService;

        public OutfitService(IUnitOfWork unitOfWork, IMapper mapper, IUserService userService, IGeminiService geminiService, IQdrantService qdrantService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userService = userService;
            _geminiService = geminiService;
            _qdrantService = qdrantService;
        }

        public async Task<BaseResponseModel> GetOutfitByIdAsync(long id, long userId)
        {
            var outfit = await _unitOfWork.OutfitRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(o => o.User)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category));

            if (outfit == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_NOT_FOUND);
            }

            // Check if the outfit belongs to the current user
            if (outfit.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.OUTFIT_ACCESS_DENIED);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_GET_SUCCESS,
                Data = _mapper.Map<OutfitDetailedModel>(outfit)
            };
        }

        public async Task<BaseResponseModel> ToggleOutfitFavoriteAsync(long id, long userId)
        {
            var outfit = await _unitOfWork.OutfitRepository.GetByIdAsync(id);

            if (outfit == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_NOT_FOUND);
            }

            // Check if the outfit belongs to the current user
            if (outfit.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.OUTFIT_ACCESS_DENIED);
            }

            // Toggle the favorite status
            outfit.IsFavorite = !outfit.IsFavorite;
            _unitOfWork.OutfitRepository.UpdateAsync(outfit);
            await _unitOfWork.SaveAsync();

            var updatedOutfit = await _unitOfWork.OutfitRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(o => o.User)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_TOGGLE_FAVORITE_SUCCESS,
                Data = _mapper.Map<OutfitModel>(updatedOutfit)
            };
        }

        public async Task<BaseResponseModel> GetAllOutfitPaginationAsync(PaginationParameter paginationParameter)
        {
            var outfits = await _unitOfWork.OutfitRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query.Include(x => x.User)
                    .Include(x => x.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category),
                filter: string.IsNullOrWhiteSpace(paginationParameter.Search)
                    ? null
                    : x => (x.Name != null && x.Name.Contains(paginationParameter.Search)) ||
                           (x.Description != null && x.Description.Contains(paginationParameter.Search)),
                orderBy: x => x.OrderByDescending(x => x.CreatedDate));

            var outfitModels = _mapper.Map<Pagination<OutfitModel>>(outfits);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_OUTFIT_SUCCESS,
                Data = new ModelPaging
                {
                    Data = outfitModels,
                    MetaData = new
                    {
                        outfitModels.TotalCount,
                        outfitModels.PageSize,
                        outfitModels.CurrentPage,
                        outfitModels.TotalPages,
                        outfitModels.HasNext,
                        outfitModels.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetOutfitByUserPaginationAsync(PaginationParameter paginationParameter, long userId, bool? isFavorite, bool? isSaved)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var outfits = await _unitOfWork.OutfitRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query.Include(x => x.User)
                    .Include(x => x.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category),
                filter: x => x.UserId == userId &&
                           (string.IsNullOrWhiteSpace(paginationParameter.Search) ||
                            (x.Name != null && x.Name.Contains(paginationParameter.Search)) ||
                            (x.Description != null && x.Description.Contains(paginationParameter.Search))) &&
                           (!isFavorite.HasValue || x.IsFavorite == isFavorite.Value) &&
                           (!isSaved.HasValue || x.IsSaved == isSaved.Value),
                orderBy: x => x.OrderByDescending(x => x.CreatedDate));

            var outfitModels = _mapper.Map<Pagination<OutfitModel>>(outfits);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_OUTFIT_SUCCESS,
                Data = new ModelPaging
                {
                    Data = outfitModels,
                    MetaData = new
                    {
                        outfitModels.TotalCount,
                        outfitModels.PageSize,
                        outfitModels.CurrentPage,
                        outfitModels.TotalPages,
                        outfitModels.HasNext,
                        outfitModels.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> CreateOutfitAsync(long userId, OutfitCreateModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (model.ItemIds != null && model.ItemIds.Any())
            {
                // Validate that all items exist and belong to the user
                foreach (var itemId in model.ItemIds)
                {
                    var item = await _unitOfWork.ItemRepository.GetByIdAsync(itemId);
                    if (item == null)
                    {
                        throw new NotFoundException($"Item with ID {itemId} not found");
                    }

                    // Validate that the item belongs to the user
                    if (item.UserId != userId)
                    {
                        throw new ForbiddenException($"Item with ID {itemId} does not belong to you. You can only create outfits with your own items.");
                    }
                }

                var sortedItemIds = model.ItemIds.OrderBy(id => id).ToList();

                var existingOutfits = await _unitOfWork.OutfitRepository.ToPaginationIncludeAsync(
                    new PaginationParameter { TakeAll = true },
                    include: query => query.Include(o => o.OutfitItems),
                    filter: o => o.UserId == userId);

                foreach (var existingOutfit in existingOutfits)
                {
                    var existingItemIds = existingOutfit.OutfitItems
                        .Where(oi => oi.ItemId.HasValue && !oi.IsDeleted)
                        .Select(oi => oi.ItemId.Value)
                        .OrderBy(id => id)
                        .ToList();

                    if (existingItemIds.Count == sortedItemIds.Count &&
                        existingItemIds.SequenceEqual(sortedItemIds))
                    {
                        throw new BadRequestException(MessageConstants.OUTFIT_DUPLICATE_ITEMS);
                    }
                }
            }

            var outfit = new Outfit
            {
                UserId = userId,
                Name = model.Name,
                Description = model.Description,
                IsFavorite = false,
                CreatedBy = OutfitCreatedBy.USER,
                IsSaved = false
            };

            await _unitOfWork.OutfitRepository.AddAsync(outfit);
            await _unitOfWork.SaveAsync();

            // Add items to OutfitItems
            if (model.ItemIds != null && model.ItemIds.Any())
            {
                foreach (var itemId in model.ItemIds)
                {
                    var outfitItem = new OutfitItem
                    {
                        OutfitId = outfit.Id,
                        ItemId = itemId
                    };
                    await _unitOfWork.OutfitItemRepository.AddAsync(outfitItem);
                }
                await _unitOfWork.SaveAsync();
            }

            var createdOutfit = await _unitOfWork.OutfitRepository.GetByIdIncludeAsync(
                outfit.Id,
                include: query => query
                    .Include(o => o.User)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.OUTFIT_CREATE_SUCCESS,
                Data = _mapper.Map<OutfitDetailedModel>(createdOutfit)
            };
        }

        public async Task<BaseResponseModel> UpdateOutfitAsync(long id, long userId, OutfitUpdateModel model)
        {
            var outfit = await _unitOfWork.OutfitRepository.GetByIdAsync(id);
            if (outfit == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_NOT_FOUND);
            }

            // Check if the outfit belongs to the current user
            if (outfit.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.OUTFIT_ACCESS_DENIED);
            }

            outfit.Name = model.Name;
            outfit.Description = model.Description;

            _unitOfWork.OutfitRepository.UpdateAsync(outfit);
            await _unitOfWork.SaveAsync();

            // Update items if provided
            if (model.ItemIds != null)
            {
                // Validate that all items exist and belong to the user
                if (model.ItemIds.Any())
                {
                    foreach (var itemId in model.ItemIds)
                    {
                        var item = await _unitOfWork.ItemRepository.GetByIdAsync(itemId);
                        if (item == null)
                        {
                            throw new NotFoundException($"Item with ID {itemId} not found");
                        }

                        // Validate that the item belongs to the user
                        if (item.UserId != userId)
                        {
                            throw new ForbiddenException($"Item with ID {itemId} does not belong to you. You can only use your own items in outfits.");
                        }
                    }
                }

                // Check for duplicate outfit with new items (only if items are being updated)
                if (model.ItemIds.Any())
                {
                    var sortedItemIds = model.ItemIds.OrderBy(x => x).ToList();

                    var existingOutfits = await _unitOfWork.OutfitRepository.ToPaginationIncludeAsync(
                        new PaginationParameter { TakeAll = true },
                        include: query => query.Include(o => o.OutfitItems),
                        filter: o => o.UserId == userId && o.Id != id);

                    foreach (var existingOutfit in existingOutfits)
                    {
                        var existingItemIds = existingOutfit.OutfitItems
                            .Where(oi => oi.ItemId.HasValue)
                            .Select(oi => oi.ItemId.Value)
                            .OrderBy(x => x)
                            .ToList();

                        if (existingItemIds.Count == sortedItemIds.Count &&
                            existingItemIds.SequenceEqual(sortedItemIds))
                        {
                            throw new BadRequestException(MessageConstants.OUTFIT_DUPLICATE_ITEMS);
                        }
                    }
                }

                // Remove existing items
                //var existingItems = await _context.Set<OutfitItems>()
                //    .Where(oi => oi.OutfitId == id)
                //    .ToListAsync();
                //_context.Set<OutfitItems>().RemoveRange(existingItems);
                var outfitExisted = await _unitOfWork.OutfitRepository.GetByIdIncludeAsync(
                    id,
                    include: query => query.Include(oi => oi.OutfitItems));

                _unitOfWork.OutfitItemRepository.SoftDeleteRangeAsync(outfitExisted.OutfitItems.ToList());

                // Add new items
                foreach (var itemId in model.ItemIds)
                {
                    var outfitItem = new OutfitItem
                    {
                        OutfitId = id,
                        ItemId = itemId
                    };
                    await _unitOfWork.OutfitItemRepository.AddAsync(outfitItem);
                }

                await _unitOfWork.SaveAsync();
            }

            var updatedOutfit = await _unitOfWork.OutfitRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(o => o.User)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_UPDATE_SUCCESS,
                Data = _mapper.Map<OutfitDetailedModel>(updatedOutfit)
            };
        }

        public async Task<BaseResponseModel> DeleteOutfitAsync(long id, long userId)
        {
            var outfit = await _unitOfWork.OutfitRepository.GetByIdAsync(id);
            if (outfit == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_NOT_FOUND);
            }

            // Check if the outfit belongs to the current user
            if (outfit.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.OUTFIT_ACCESS_DENIED);
            }

            _unitOfWork.OutfitRepository.SoftDeleteAsync(outfit);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_DELETE_SUCCESS
            };
        }

        public async Task<BaseResponseModel> ToggleOutfitSaveAsync(long id, long userId)
        {
            var outfit = await _unitOfWork.OutfitRepository.GetByIdAsync(id);

            if (outfit == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_NOT_FOUND);
            }

            // Check if the outfit belongs to the current user
            if (outfit.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.OUTFIT_ACCESS_DENIED);
            }

            // Toggle the save status
            outfit.IsSaved = !outfit.IsSaved;
            _unitOfWork.OutfitRepository.UpdateAsync(outfit);
            await _unitOfWork.SaveAsync();

            var updatedOutfit = await _unitOfWork.OutfitRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(o => o.User)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_TOGGLE_SAVE_SUCCESS,
                Data = _mapper.Map<OutfitModel>(updatedOutfit)
            };
        }


        public async Task<BaseResponseModel> GetOutfitCalendarPaginationAsync(
            PaginationParameter paginationParameter,
            long userId,
            CalendarFilterType? filterType = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? year = null,
            int? month = null)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            DateTime? filterStartDate = null;
            DateTime? filterEndDate = null;
            var today = DateTime.Today;

            // Handle filter by CalendarFilterType enum
            if (filterType.HasValue)
            {
                switch (filterType.Value)
                {
                    case CalendarFilterType.THIS_WEEK:
                        // Get current week (Monday to Sunday)
                        var dayOfWeek = (int)today.DayOfWeek;
                        // If Sunday (0), treat as 7 for calculation
                        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
                        filterStartDate = today.AddDays(-daysFromMonday);
                        filterEndDate = filterStartDate.Value.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
                        break;

                    case CalendarFilterType.THIS_MONTH:
                        // Get current month
                        filterStartDate = new DateTime(today.Year, today.Month, 1);
                        filterEndDate = filterStartDate.Value.AddMonths(1).AddTicks(-1);
                        break;

                    case CalendarFilterType.SPECIFIC_MONTH:
                        // Requires year and month parameters
                        if (!year.HasValue || !month.HasValue)
                        {
                            throw new BadRequestException("Year and Month are required when using SPECIFIC_MONTH filter");
                        }
                        if (month.Value < 1 || month.Value > 12)
                        {
                            throw new BadRequestException("Month must be between 1 and 12");
                        }
                        filterStartDate = new DateTime(year.Value, month.Value, 1);
                        filterEndDate = filterStartDate.Value.AddMonths(1).AddTicks(-1);
                        break;

                    case CalendarFilterType.DATE_RANGE:
                        // Requires startDate and endDate parameters
                        if (!startDate.HasValue || !endDate.HasValue)
                        {
                            throw new BadRequestException("StartDate and EndDate are required when using DATE_RANGE filter");
                        }
                        filterStartDate = startDate.Value.Date;
                        filterEndDate = endDate.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                        break;
                }
            }
            else
            {
                // Legacy support: fallback to old parameter-based filtering
                if (year.HasValue && month.HasValue)
                {
                    // Get entries for specific month and year
                    if (month.Value < 1 || month.Value > 12)
                    {
                        throw new BadRequestException("Month must be between 1 and 12");
                    }
                    filterStartDate = new DateTime(year.Value, month.Value, 1);
                    filterEndDate = filterStartDate.Value.AddMonths(1).AddTicks(-1);
                }
                else if (year.HasValue)
                {
                    // Get entries for entire year
                    filterStartDate = new DateTime(year.Value, 1, 1);
                    filterEndDate = new DateTime(year.Value, 12, 31, 23, 59, 59);
                }
                else if (startDate.HasValue || endDate.HasValue)
                {
                    // Use provided date range
                    filterStartDate = startDate;
                    filterEndDate = endDate;
                }
            }

            // Get all user occasions (including those without outfits)
            var allUserOccasions = _unitOfWork.UserOccasionRepository.GetQueryable()
                .Include(x => x.User)
                .Include(x => x.Occasion)
                .Include(x => x.OutfitUsageHistories)
                    .ThenInclude(ouh => ouh.Outfit)
                        .ThenInclude(o => o.User)
                .Include(x => x.OutfitUsageHistories)
                    .ThenInclude(ouh => ouh.Outfit)
                        .ThenInclude(o => o.OutfitItems)
                            .ThenInclude(oi => oi.Item)
                                .ThenInclude(i => i.Category)
                .Where(x => x.UserId == userId &&
                           (!filterStartDate.HasValue || x.DateOccasion >= filterStartDate.Value) &&
                           (!filterEndDate.HasValue || x.DateOccasion <= filterEndDate.Value))
                .OrderBy(x => x.DateOccasion)
                .ThenBy(x => x.StartTime)
                .ToList();

            // Group by UserOccasion
            // For daily occasions (IsDaily = true), group by date
            // For specific occasions (IsDaily = false), each occasion is its own group
            var grouped = allUserOccasions
                .GroupBy(x => new
                {
                    IsDaily = x.Name == "Daily",
                    GroupKey = x.Name == "Daily"
                        ? x.DateOccasion.Date.ToString("yyyy-MM-dd") // Group daily by date
                        : x.Id.ToString() // Group specific by ID
                })
                .Select(g => g.First())
                .Select(uo => _mapper.Map<OutfitCalendarGroupedModel>(uo))
                .ToList();

            // Apply pagination to grouped results
            var totalCount = grouped.Count;
            var pagedGroups = grouped
                .Skip((paginationParameter.PageIndex - 1) * paginationParameter.PageSize)
                .Take(paginationParameter.PageSize)
                .ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)paginationParameter.PageSize);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_OUTFIT_CALENDAR_SUCCESS,
                Data = new ModelPaging
                {
                    Data = pagedGroups,
                    MetaData = new
                    {
                        TotalCount = totalCount,
                        PageSize = paginationParameter.PageSize,
                        CurrentPage = paginationParameter.PageIndex,
                        TotalPages = totalPages,
                        HasNext = paginationParameter.PageIndex < totalPages,
                        HasPrevious = paginationParameter.PageIndex > 1
                    }
                }
            };
        }

        public async Task<BaseResponseModel> GetOutfitCalendarByIdAsync(long id, long userId)
        {
            var outfitCalendar = await _unitOfWork.OutfitRepository.GetOutfitCalendarByIdAsync(id);

            if (outfitCalendar == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_CALENDAR_NOT_FOUND);
            }

            if (outfitCalendar.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.OUTFIT_CALENDAR_ACCESS_DENIED);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_CALENDAR_GET_SUCCESS,
                Data = _mapper.Map<OutfitCalendarDetailedModel>(outfitCalendar)
            };
        }

        public async Task<BaseResponseModel> GetOutfitCalendarByUserOccasionIdAsync(long userOccasionId, long userId)
        {
            // Validate user occasion exists and belongs to user
            var userOccasion = await _unitOfWork.UserOccasionRepository.GetByIdAsync(userOccasionId);
            if (userOccasion == null)
            {
                throw new NotFoundException(MessageConstants.USER_OCCASION_NOT_FOUND);
            }

            if (userOccasion.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.USER_OCCASION_ACCESS_DENIED);
            }

            // Get all outfit calendar entries for this user occasion
            var outfitCalendars = await _unitOfWork.OutfitRepository.GetOutfitCalendarByUserOccasionAsync(userOccasionId, userId);

            var models = _mapper.Map<List<OutfitCalendarDetailedModel>>(outfitCalendars);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_OUTFIT_CALENDAR_SUCCESS,
                Data = models
            };
        }

        public async Task<BaseResponseModel> CreateOutfitCalendarAsync(long userId, OutfitCalendarCreateModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Validate IsDaily logic
            if (model.IsDaily)
            {
                // IsDaily = true: Time is required, UserOccasionId should NOT be provided
                if (!model.Time.HasValue)
                {
                    throw new BadRequestException("Time is required when IsDaily is true");
                }

                if (model.UserOccasionId.HasValue)
                {
                    throw new BadRequestException("UserOccasionId should not be provided when IsDaily is true. The Daily occasion will be auto-created.");
                }
            }
            else
            {
                // IsDaily = false: UserOccasionId is required, Time should NOT be provided
                if (!model.UserOccasionId.HasValue)
                {
                    throw new BadRequestException("UserOccasionId is required when IsDaily is false");
                }

                if (model.Time.HasValue)
                {
                    throw new BadRequestException("Time should not be provided when IsDaily is false. Use the UserOccasion's time instead.");
                }
            }

            // Validate all outfits exist and belong to user
            var outfits = new List<Outfit>();
            foreach (var outfitId in model.OutfitIds)
            {
                var outfit = await _unitOfWork.OutfitRepository.GetByIdAsync(outfitId);
                if (outfit == null)
                {
                    throw new NotFoundException($"Outfit with ID {outfitId} not found");
                }

                if (outfit.UserId != userId)
                {
                    throw new ForbiddenException($"Access denied to outfit {outfitId}");
                }

                outfits.Add(outfit);
            }

            // Handle Daily outfit logic
            long? userOccasionId = null;

            if (model.IsDaily)
            {
                // Always create a new "Daily" UserOccasion with the provided time
                var dailyOccasion = new UserOccasion
                {
                    UserId = userId,
                    Name = "Daily",
                    Description = "Daily outfit schedule",
                    DateOccasion = model.Time.Value.Date,
                    StartTime = model.Time.Value,
                    EndTime = model.EndTime,
                    OccasionId = null
                };
                await _unitOfWork.UserOccasionRepository.AddAsync(dailyOccasion);
                await _unitOfWork.SaveAsync();

                userOccasionId = dailyOccasion.Id;
            }
            else
            {
                // Verify user occasion exists and belongs to user
                var userOccasion = await _unitOfWork.UserOccasionRepository.GetByIdAsync(model.UserOccasionId.Value);
                if (userOccasion == null)
                {
                    throw new NotFoundException(MessageConstants.USER_OCCASION_NOT_FOUND);
                }

                if (userOccasion.UserId != userId)
                {
                    throw new ForbiddenException(MessageConstants.USER_OCCASION_ACCESS_DENIED);
                }

                userOccasionId = model.UserOccasionId.Value;
            }

            // Create calendar entries for all outfits
            var createdCalendars = new List<OutfitCalendarModel>();

            foreach (var outfitId in model.OutfitIds)
            {
                var outfitCalendar = new OutfitUsageHistory
                {
                    UserId = userId,
                    OutfitId = outfitId,
                    UserOccassionId = userOccasionId,
                    CreatedBy = OutfitCreatedBy.USER
                };

                await _unitOfWork.OutfitRepository.AddOutfitCalendarAsync(outfitCalendar);
                await _unitOfWork.SaveAsync();

                var created = await _unitOfWork.OutfitRepository.GetOutfitCalendarByIdAsync(outfitCalendar.Id);
                createdCalendars.Add(_mapper.Map<OutfitCalendarModel>(created));
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.OUTFIT_CALENDAR_CREATE_SUCCESS,
                Data = createdCalendars
            };
        }

        public async Task<BaseResponseModel> UpdateOutfitCalendarAsync(long id, long userId, OutfitCalendarUpdateModel model)
        {
            var outfitCalendar = await _unitOfWork.OutfitRepository.GetOutfitCalendarByIdAsync(id);
            if (outfitCalendar == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_CALENDAR_NOT_FOUND);
            }

            if (outfitCalendar.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.OUTFIT_CALENDAR_ACCESS_DENIED);
            }

            // Validate IsDaily logic if provided
            if (model.IsDaily.HasValue)
            {
                if (model.IsDaily.Value)
                {
                    // IsDaily = true: Time is required, UserOccasionId should NOT be provided
                    if (!model.Time.HasValue)
                    {
                        throw new BadRequestException("Time is required when IsDaily is true");
                    }

                    if (model.UserOccasionId.HasValue)
                    {
                        throw new BadRequestException("UserOccasionId should not be provided when IsDaily is true. The Daily occasion will be auto-created.");
                    }
                }
                else
                {
                    // IsDaily = false: UserOccasionId is required, Time should NOT be provided
                    if (!model.UserOccasionId.HasValue)
                    {
                        throw new BadRequestException("UserOccasionId is required when IsDaily is false");
                    }

                    if (model.Time.HasValue)
                    {
                        throw new BadRequestException("Time should not be provided when IsDaily is false. Use the UserOccasion's time instead.");
                    }
                }
            }

            // Verify outfit if provided
            if (model.OutfitId.HasValue)
            {
                var outfit = await _unitOfWork.OutfitRepository.GetByIdAsync(model.OutfitId.Value);
                if (outfit == null)
                {
                    throw new NotFoundException(MessageConstants.OUTFIT_NOT_FOUND);
                }

                if (outfit.UserId != userId)
                {
                    throw new ForbiddenException(MessageConstants.OUTFIT_ACCESS_DENIED);
                }

                outfitCalendar.OutfitId = model.OutfitId.Value;
            }

            // Handle Daily outfit logic
            if (model.IsDaily.HasValue && model.IsDaily.Value)
            {
                // Always create a new "Daily" UserOccasion with the provided time
                var dailyOccasion = new UserOccasion
                {
                    UserId = userId,
                    Name = "Daily",
                    Description = "Daily outfit schedule",
                    DateOccasion = model.Time.Value.Date,
                    StartTime = model.Time.Value,
                    EndTime = model.EndTime,
                    OccasionId = null
                };
                await _unitOfWork.UserOccasionRepository.AddAsync(dailyOccasion);
                await _unitOfWork.SaveAsync();

                // Set the UserOccasionId to the Daily occasion
                outfitCalendar.UserOccassionId = dailyOccasion.Id;
            }
            else if (model.UserOccasionId.HasValue)
            {
                // Verify user occasion exists and belongs to user
                var userOccasion = await _unitOfWork.UserOccasionRepository.GetByIdAsync(model.UserOccasionId.Value);
                if (userOccasion == null)
                {
                    throw new NotFoundException(MessageConstants.USER_OCCASION_NOT_FOUND);
                }

                if (userOccasion.UserId != userId)
                {
                    throw new ForbiddenException(MessageConstants.USER_OCCASION_ACCESS_DENIED);
                }

                outfitCalendar.UserOccassionId = model.UserOccasionId.Value;
            }

            _unitOfWork.OutfitRepository.UpdateOutfitCalendar(outfitCalendar);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.OutfitRepository.GetOutfitCalendarByIdAsync(id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_CALENDAR_UPDATE_SUCCESS,
                Data = _mapper.Map<OutfitCalendarModel>(updated)
            };
        }

        public async Task<BaseResponseModel> DeleteOutfitCalendarAsync(long id, long userId)
        {
            var outfitCalendar = await _unitOfWork.OutfitRepository.GetOutfitCalendarByIdAsync(id);
            if (outfitCalendar == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_CALENDAR_NOT_FOUND);
            }

            // Check if the outfit calendar entry belongs to the current user
            if (outfitCalendar.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.OUTFIT_CALENDAR_ACCESS_DENIED);
            }

            _unitOfWork.OutfitRepository.DeleteOutfitCalendar(outfitCalendar);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_CALENDAR_DELETE_SUCCESS
            };
        }



        private string BuildUserCharacteristicString(UserCharacteristicModel userCharacteristic)
        {
            if (userCharacteristic == null) return string.Empty;

            var characteristics = new List<string>();

            if (!string.IsNullOrWhiteSpace(userCharacteristic.DisplayName))
                characteristics.Add($"DisplayName: {userCharacteristic.DisplayName}");

            if (userCharacteristic.Dob.HasValue)
                characteristics.Add($"DateOfBirth: {userCharacteristic.Dob.Value:yyyy-MM-dd}");

            characteristics.Add($"Gender: {userCharacteristic.Gender}");

            if (!string.IsNullOrWhiteSpace(userCharacteristic.Location))
                characteristics.Add($"Location: {userCharacteristic.Location}");

            if (!string.IsNullOrWhiteSpace(userCharacteristic.Bio))
                characteristics.Add($"Bio: {userCharacteristic.Bio}");

            if (!string.IsNullOrWhiteSpace(userCharacteristic.Job))
                characteristics.Add($"Job: {userCharacteristic.Job}");

            if (userCharacteristic.Styles != null && userCharacteristic.Styles.Any())
                characteristics.Add($"PreferredStyles: {string.Join(", ", userCharacteristic.Styles)}");

            if (userCharacteristic.PreferedColor != null && userCharacteristic.PreferedColor.Any())
                characteristics.Add($"PreferredColors: {string.Join(", ", userCharacteristic.PreferedColor)}");

            if (userCharacteristic.AvoidedColor != null && userCharacteristic.AvoidedColor.Any())
                characteristics.Add($"AvoidedColors: {string.Join(", ", userCharacteristic.AvoidedColor)}");

            return string.Join("; ", characteristics);
        }

        private string BuildOccasionString(UserOccasion userOccasion)
        {
            var occasionDetails = new List<string>();

            if (!string.IsNullOrWhiteSpace(userOccasion.Name))
                occasionDetails.Add($"EventName: {userOccasion.Name}");

            if (userOccasion.Occasion != null && !string.IsNullOrWhiteSpace(userOccasion.Occasion.Name))
                occasionDetails.Add($"OccasionType: {userOccasion.Occasion.Name}");

            if (!string.IsNullOrWhiteSpace(userOccasion.Description))
                occasionDetails.Add($"Description: {userOccasion.Description}");

            occasionDetails.Add($"Date: {userOccasion.DateOccasion:yyyy-MM-dd}");

            if (userOccasion.StartTime.HasValue)
                occasionDetails.Add($"StartTime: {userOccasion.StartTime.Value:HH:mm}");

            if (userOccasion.EndTime.HasValue)
                occasionDetails.Add($"EndTime: {userOccasion.EndTime.Value:HH:mm}");

            if (!string.IsNullOrWhiteSpace(userOccasion.WeatherSnapshot))
                occasionDetails.Add($"Weather: {userOccasion.WeatherSnapshot}");

            return string.Join("; ", occasionDetails);
        }

        public async Task<BaseResponseModel> OutfitSuggestion(long userId, long? occasionId, string? weather, string? userInstruction)
        {
            var overallStopwatch = Stopwatch.StartNew();
            
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Build occasion string
            var occasionStopwatch = Stopwatch.StartNew();
            string occasionString = string.Empty;
            if (occasionId.HasValue)
            {
                var occasion = await _unitOfWork.UserOccasionRepository.GetByIdAsync(occasionId.Value);
                if (occasion == null)
                {
                    throw new NotFoundException(MessageConstants.USER_OCCASION_NOT_FOUND);
                }

                if (occasion.UserId != userId)
                {
                    throw new ForbiddenException(MessageConstants.USER_OCCASION_ACCESS_DENIED);
                }

                occasionString = BuildOccasionString(occasion);
            }
            occasionStopwatch.Stop();
            Console.WriteLine($"[TIMING] Build occasion string: {occasionStopwatch.ElapsedMilliseconds}ms");

            // Get user characteristics
            var characteristicStopwatch = Stopwatch.StartNew();
            var userCharacteristic = await _userService.GetUserCharacteristic(userId);
            string characteristicString = BuildUserCharacteristicString(userCharacteristic);
            characteristicStopwatch.Stop();
            Console.WriteLine($"[TIMING] Get user characteristics: {characteristicStopwatch.ElapsedMilliseconds}ms");

            // Get outfit suggestions from Gemini
            var geminiStopwatch = Stopwatch.StartNew();
            var listItem = await _geminiService.OutfitSuggestion(occasionString, characteristicString, weather, userInstruction);
            listItem.ForEach(x => Console.WriteLine(x));
            geminiStopwatch.Stop();
            Console.WriteLine($"[TIMING] Gemini outfit suggestion: {geminiStopwatch.ElapsedMilliseconds}ms");

            // Execute searches in parallel
            var searchStopwatch = Stopwatch.StartNew();
            var searchTasks = listItem.Select(item => _qdrantService.SearchItemIdsByUserId(item, userId)).ToList();
            var searchResults = await Task.WhenAll(searchTasks);
            var allItemIds = searchResults.SelectMany(result => result).ToList();
            searchStopwatch.Stop();
            Console.WriteLine($"[TIMING] Qdrant parallel search ({listItem.Count} queries): {searchStopwatch.ElapsedMilliseconds}ms");

            // Get all items by IDs
            var dbStopwatch = Stopwatch.StartNew();
            var items = await _unitOfWork.ItemRepository.GetItemsByIdsAsync(allItemIds.Select(x => x.ItemId).ToList());
            dbStopwatch.Stop();
            Console.WriteLine($"[TIMING] Database query for items ({allItemIds.Count} ids): {dbStopwatch.ElapsedMilliseconds}ms");

            // Map items to QDrantSearchModels
            var mappingStopwatch = Stopwatch.StartNew();
            var listPartItems = items.Select(item =>
            {
                var itemModel = _mapper.Map<ItemModel>(item);
                var searchModel = new QDrantSearchModels
                {
                    Id = (int)item.Id,
                    ItemName = itemModel.Name,
                    ImgURL = itemModel.ImgUrl,
                    Colors = string.IsNullOrEmpty(itemModel.Color)
                        ? new List<ColorModel>()
                        : JsonSerializer.Deserialize<List<ColorModel>>(itemModel.Color),
                    AiDescription = itemModel.AiDescription,
                    WeatherSuitable = itemModel.WeatherSuitable,
                    Condition = itemModel.Condition,
                    Pattern = itemModel.Pattern,
                    Fabric = itemModel.Fabric,
                    Styles = itemModel.Styles,
                    Occasions = itemModel.Occasions,
                    Seasons = itemModel.Seasons,
                    Confidence = itemModel.AIConfidence,
                    Score = allItemIds.FirstOrDefault(x => x.ItemId == item.Id)?.Score ?? 0
                };
                return searchModel;
            }).ToList();
            mappingStopwatch.Stop();
            Console.WriteLine($"[TIMING] Map items to search models ({items.Count} items): {mappingStopwatch.ElapsedMilliseconds}ms");

            // Choose outfit from the search results
            var chooseOutfitStopwatch = Stopwatch.StartNew();
            var outfitSelection = await _geminiService.ChooseOutfit(occasionString, characteristicString, listPartItems, weather, userInstruction);
            chooseOutfitStopwatch.Stop();
            Console.WriteLine($"[TIMING] Gemini choose outfit: {chooseOutfitStopwatch.ElapsedMilliseconds}ms");

            // Fetch full item details for the selected items
            var fetchItemsStopwatch = Stopwatch.StartNew();
            var selectedItems = new List<ItemModel>();
            foreach (var itemId in outfitSelection.ItemIds)
            {
                var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(itemId,
                    include: query => query.Include(x => x.Category)
                                           .Include(x => x.User)
                                           .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                                           .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                                           .Include(x => x.ItemStyles).ThenInclude(x => x.Style));
                
                if (item != null)
                {
                    selectedItems.Add(_mapper.Map<ItemModel>(item));
                }
            }
            fetchItemsStopwatch.Stop();
            Console.WriteLine($"[TIMING] Fetch full item details ({selectedItems.Count} items): {fetchItemsStopwatch.ElapsedMilliseconds}ms");
            
            overallStopwatch.Stop();
            Console.WriteLine($"[TIMING] *** TOTAL EXECUTION TIME: {overallStopwatch.ElapsedMilliseconds}ms ***");
            
            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_SUGGESTION_SUCCESS,
                Data = new
                {
                    Items = selectedItems,
                    Reason = outfitSelection.Reason
                }
            };
        }
    }
}
