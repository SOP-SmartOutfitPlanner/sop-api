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
using SOPServer.Service.BusinessModels.VirtualTryOnModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace SOPServer.Service.Services.Implements
{
    public class OutfitService : IOutfitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IGeminiService _geminiService;
        private readonly IQdrantService _qdrantService;
        private readonly IHttpClientFactory _httpClientFactory;

        public OutfitService(IUnitOfWork unitOfWork, IMapper mapper, IUserService userService, IGeminiService geminiService, IQdrantService qdrantService, IHttpClientFactory httpClientFactory)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userService = userService;
            _geminiService = geminiService;
            _qdrantService = qdrantService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<BaseResponseModel> GetOutfitByIdAsync(long id, long userId)
        {
            var outfit = await _unitOfWork.OutfitRepository.GetByIdIncludeAsync(
                id,
                include: query => query
                    .Include(o => o.User)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemOccasions.Where(io => !io.IsDeleted))
                                .ThenInclude(io => io.Occasion)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemSeasons.Where(iSeason => !iSeason.IsDeleted))
                                .ThenInclude(iSeason => iSeason.Season)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemStyles.Where(iStyle => !iStyle.IsDeleted))
                                .ThenInclude(iStyle => iStyle.Style));

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
                            .ThenInclude(i => i.Category)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemOccasions.Where(io => !io.IsDeleted))
                                .ThenInclude(io => io.Occasion)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemSeasons.Where(iSeason => !iSeason.IsDeleted))
                                .ThenInclude(iSeason => iSeason.Season)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemStyles.Where(iStyle => !iStyle.IsDeleted))
                                .ThenInclude(iStyle => iStyle.Style));

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
                include: query => query
                    .Include(x => x.User)
                    .Include(x => x.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category)
                    .Include(x => x.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemOccasions.Where(io => !io.IsDeleted))
                                .ThenInclude(io => io.Occasion)
                    .Include(x => x.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemSeasons.Where(iSeason => !iSeason.IsDeleted))
                                .ThenInclude(iSeason => iSeason.Season)
                    .Include(x => x.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemStyles.Where(iStyle => !iStyle.IsDeleted))
                                .ThenInclude(iStyle => iStyle.Style),
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

        public async Task<BaseResponseModel> GetOutfitByUserPaginationAsync(
                                                                            PaginationParameter paginationParameter,
                                                                            long userId,
                                                                            bool? isFavorite,
                                                                            bool? isSaved,
                                                                            DateTime? startDate,
                                                                            DateTime? endDate)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var outfits = await _unitOfWork.OutfitRepository.ToPaginationIncludeAsync(
                paginationParameter,
                include: query => query
                    .Include(x => x.User)
                    .Include(x => x.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category)
                    .Include(x => x.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemOccasions.Where(io => !io.IsDeleted))
                                .ThenInclude(io => io.Occasion)
                    .Include(x => x.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemSeasons.Where(iSeason => !iSeason.IsDeleted))
                                .ThenInclude(iSeason => iSeason.Season)
                    .Include(x => x.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemStyles.Where(iStyle => !iStyle.IsDeleted))
                                .ThenInclude(iStyle => iStyle.Style),
                filter: x => x.UserId == userId &&
                           (string.IsNullOrWhiteSpace(paginationParameter.Search) ||
                            (x.Name != null && x.Name.Contains(paginationParameter.Search)) ||
                            (x.Description != null && x.Description.Contains(paginationParameter.Search))) &&
                           (!isFavorite.HasValue || x.IsFavorite == isFavorite.Value) &&
                           (!isSaved.HasValue || x.IsSaved == isSaved.Value) &&
                           (!startDate.HasValue || x.CreatedDate >= startDate.Value) &&
                           (!endDate.HasValue || x.CreatedDate <= endDate.Value),
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
                foreach (var itemId in model.ItemIds)
                {
                    var item = await _unitOfWork.ItemRepository.GetByIdAsync(itemId);
                    if (item == null)
                    {
                        throw new NotFoundException($"Item with ID {itemId} not found");
                    }

                    if (item.ItemType != ItemType.SYSTEM && item.UserId != userId)
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
                            .ThenInclude(i => i.Category)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemOccasions.Where(io => !io.IsDeleted))
                                .ThenInclude(io => io.Occasion)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemSeasons.Where(iSeason => !iSeason.IsDeleted))
                                .ThenInclude(iSeason => iSeason.Season)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemStyles.Where(iStyle => !iStyle.IsDeleted))
                                .ThenInclude(iStyle => iStyle.Style));

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
                            .ThenInclude(i => i.Category)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemOccasions.Where(io => !io.IsDeleted))
                                .ThenInclude(io => io.Occasion)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemSeasons.Where(iSeason => !iSeason.IsDeleted))
                                .ThenInclude(iSeason => iSeason.Season)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemStyles.Where(iStyle => !iStyle.IsDeleted))
                                .ThenInclude(iStyle => iStyle.Style));

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
                            .ThenInclude(i => i.Category)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemOccasions.Where(io => !io.IsDeleted))
                                .ThenInclude(io => io.Occasion)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemSeasons.Where(iSeason => !iSeason.IsDeleted))
                                .ThenInclude(iSeason => iSeason.Season)
                    .Include(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemStyles.Where(iStyle => !iStyle.IsDeleted))
                                .ThenInclude(iStyle => iStyle.Style));

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
                var targetDate = model.Time.Value.Date;

                // Check if a "Daily" UserOccasion already exists for this user on this date
                var existingDailyOccasions = await _unitOfWork.UserOccasionRepository
                    .GetAllAsync();

                var existingDailyOccasion = existingDailyOccasions
                    .FirstOrDefault(uo => uo.UserId == userId
                                       && uo.Name == "Daily"
                                       && uo.OccasionId == 7
                                       && uo.DateOccasion.Date == targetDate
                                       && !uo.IsDeleted);

                if (existingDailyOccasion != null)
                {
                    // Use the existing Daily UserOccasion
                    userOccasionId = existingDailyOccasion.Id;
                }
                else
                {
                    // Create a new "Daily" UserOccasion
                    var dailyOccasion = new UserOccasion
                    {
                        UserId = userId,
                        Name = "Daily",
                        Description = "Daily outfit schedule",
                        DateOccasion = targetDate,
                        StartTime = model.Time.Value,
                        EndTime = model.EndTime,
                        OccasionId = 7
                    };
                    await _unitOfWork.UserOccasionRepository.AddAsync(dailyOccasion);
                    await _unitOfWork.SaveAsync();
                    userOccasionId = dailyOccasion.Id;
                }
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

            // Get existing outfits for this UserOccasion to prevent duplicates
            var existingOutfitCalendars = await _unitOfWork.OutfitRepository
                .GetOutfitCalendarByUserOccasionAsync(userOccasionId.Value, userId);

            var existingOutfitIds = existingOutfitCalendars
                .Select(oc => oc.OutfitId)
                .ToHashSet();

            // Create calendar entries for all outfits
            var createdCalendars = new List<OutfitCalendarModel>();
            foreach (var outfitId in model.OutfitIds)
            {
                // Check if outfit already exists in this UserOccasion
                if (existingOutfitIds.Contains(outfitId))
                {
                    throw new BadRequestException($"Outfit with ID {outfitId} is already added to this occasion");
                }

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



        private async Task ValidateUserForOutfitSuggestion(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }
        }

        private async Task<(string occasionString, UserCharacteristicModel userCharacteristic, 
            List<Season> listSeason, List<Occasion> listOccasion, List<Style> listStyle)> 
            FetchRequiredDataForOutfitSuggestion(OutfitSuggestionRequestModel model)
        {
            var dataFetchStopwatch = Stopwatch.StartNew();
            var occasionString = await GetOccasionStringAsync(model.OccasionId);
            var userCharacteristic = await _userService.GetUserCharacteristic(model.UserId);
            var listSeason = await _unitOfWork.SeasonRepository.GetAllAsync();
            var listOccasion = await _unitOfWork.OccasionRepository.GetAllAsync();
            var listStyle = await _unitOfWork.StyleRepository.getAllStyleSystem();

            dataFetchStopwatch.Stop();
            Console.WriteLine($"[TIMING] Data fetch (parallel): {dataFetchStopwatch.ElapsedMilliseconds}ms");

            return (occasionString, userCharacteristic, listSeason, listOccasion, listStyle);
        }

        private async Task<(List<long>? selectedSeasonIds, List<long>? selectedOccasionIds, List<long>? selectedStyleIds)>
            GetAISuggestionsForCategories(string occasionString, string characteristicString,
                List<Season> listSeason, List<Occasion> listOccasion, List<Style> listStyle)
        {
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var listSeasonModel = listSeason.Select(x => new { Id = x.Id, Name = x.Name }).ToList();
            var listOccasionModel = listOccasion.Select(x => new { Id = x.Id, Name = x.Name }).ToList();
            var listStyleModel = listStyle.Select(x => new { Id = x.Id, Name = x.Name }).ToList();

            var seasonJson = JsonSerializer.Serialize(listSeasonModel, serializerOptions);
            var occasionJson = JsonSerializer.Serialize(listOccasionModel, serializerOptions);
            var styleJson = JsonSerializer.Serialize(listStyleModel, serializerOptions);

            // Call Gemini AI for all categories in PARALLEL
            var geminiStopwatch = Stopwatch.StartNew();
            var selectedSeasonIdsTask = _geminiService.ItemCharacteristicSuggestion(
                seasonJson,
                occasionString,
                characteristicString
            );

            var selectedOccasionIdsTask = _geminiService.ItemCharacteristicSuggestion(
                occasionJson,
                occasionString,
                characteristicString
            );

            var selectedStyleIdsTask = _geminiService.ItemCharacteristicSuggestion(
                styleJson,
                occasionString,
                characteristicString
            );

            await Task.WhenAll(selectedSeasonIdsTask, selectedOccasionIdsTask, selectedStyleIdsTask);

            var selectedSeasonIds = await selectedSeasonIdsTask;
            var selectedOccasionIds = await selectedOccasionIdsTask;
            var selectedStyleIds = await selectedStyleIdsTask;

            geminiStopwatch.Stop();
            Console.WriteLine($"[TIMING] Gemini AI suggestions (parallel): {geminiStopwatch.ElapsedMilliseconds}ms");

            return (selectedSeasonIds, selectedOccasionIds, selectedStyleIds);
        }

        private async Task<List<ItemChooseModel>> FilterAndMapItemsForOutfitSuggestion(
            List<long>? selectedSeasonIds, List<long>? selectedOccasionIds, List<long>? selectedStyleIds, long userId)
        {
            // Fetch items matching the selected Season/Occasion/Style IDs from user's wardrobe
            // Exclude duplicate items across categories to get unique items per category
            var itemFetchStopwatch = Stopwatch.StartNew();
            
            var seasonItems = await _unitOfWork.ItemRepository.GetItemsBySeasonIdsAsync(selectedSeasonIds ?? new List<long>(), userId);
            var seasonItemIds = seasonItems.Select(i => i.Id).ToList();

            var occasionItems = await _unitOfWork.ItemRepository.GetItemsByOccasionIdsAsync(
                selectedOccasionIds ?? new List<long>(), 
                seasonItemIds, 
                userId);
            var occasionItemIds = occasionItems.Select(i => i.Id).ToList();

            var excludedIds = seasonItemIds.Concat(occasionItemIds).ToList();
            var styleItems = await _unitOfWork.ItemRepository.GetItemsByStyleIdsAsync(
                selectedStyleIds ?? new List<long>(), 
                excludedIds, 
                userId);

            itemFetchStopwatch.Stop();
            Console.WriteLine($"[TIMING] Item fetch and filtering: {itemFetchStopwatch.ElapsedMilliseconds}ms");

            // Map items to ItemModel
            var seasonItemsModel = seasonItems.Select(i => _mapper.Map<ItemChooseModel>(i)).ToList();
            var occasionItemsModel = occasionItems.Select(i => _mapper.Map<ItemChooseModel>(i)).ToList();
            var styleItemsModel = styleItems.Select(i => _mapper.Map<ItemChooseModel>(i)).ToList();
            var combinedItems = seasonItemsModel.Concat(occasionItemsModel)
                .Concat(styleItemsModel)
                .ToList();

            return combinedItems;
        }

        private async Task<OutfitSelectionModel[]> GenerateMultipleOutfitSuggestions(
            List<ItemChooseModel> combinedItems, string occasionString, string characteristicString, 
            string? weather, int totalOutfits)
        {
            // Generate multiple outfit suggestions concurrently
            var outfitGenerationStopwatch = Stopwatch.StartNew();
            
            var outfitTasks = new List<Task<OutfitSelectionModel>>();
            for (int i = 0; i < totalOutfits; i++)
            {
                var task = _geminiService.ChooseOutfitV2(
                    JsonSerializer.Serialize(combinedItems.OrderBy(x => Random.Shared.Next()).ToList()),
                    occasionString,
                    characteristicString,
                    weather
                );
                outfitTasks.Add(task);
            }

            var outfitSelections = await Task.WhenAll(outfitTasks);
            outfitGenerationStopwatch.Stop();
            Console.WriteLine($"[TIMING] Outfit generation (concurrent {totalOutfits} calls): {outfitGenerationStopwatch.ElapsedMilliseconds}ms");

            return outfitSelections;
        }

        private async Task<List<OutfitSuggestionModel>> MapOutfitSelectionsToSuggestions(OutfitSelectionModel[] outfitSelections)
        {
            var allSuggestedOutfits = new List<OutfitSuggestionModel>();
            
            foreach (var selection in outfitSelections)
            {
                var selectedItems = selection.ItemIds?.Any() == true
                    ? await _unitOfWork.ItemRepository.GetItemsByIdsAsync(selection.ItemIds)
                    : new List<Item>();

                var selectedItemModels = selectedItems.Select(item => _mapper.Map<ItemModel>(item)).ToList();
                
                allSuggestedOutfits.Add(new OutfitSuggestionModel
                {
                    SuggestedItems = selectedItemModels,
                    Reason = selection.Reason
                });
            }

            return allSuggestedOutfits;
        }

        private string BuildUserCharacteristicString(UserCharacteristicModel userCharacteristic)
        {
            if (userCharacteristic == null) return string.Empty;

            var characteristics = new List<string>();

            if (userCharacteristic.Dob.HasValue)
            {
                var today = DateTime.Today;
                var age = today.Year - userCharacteristic.Dob.Value.Year;

                characteristics.Add($"Age: {age}");
            }

            characteristics.Add($"Gender: {userCharacteristic.Gender}");

            if (!string.IsNullOrWhiteSpace(userCharacteristic.Job))
                characteristics.Add($"Job: {userCharacteristic.Job}");

            if (userCharacteristic.PreferedColor != null && userCharacteristic.PreferedColor.Any())
                characteristics.Add($"PreferredColors: {string.Join(", ", userCharacteristic.PreferedColor)}");

            return string.Join("; ", characteristics);
        }

        private async Task<string> GetOccasionStringAsync(long? occasionId)
        {
            if (!occasionId.HasValue)
                return string.Empty;

            var occasion = await _unitOfWork.OccasionRepository.GetByIdAsync(occasionId.Value);
            if (occasion == null)
                throw new NotFoundException(MessageConstants.USER_OCCASION_NOT_FOUND);

            return occasion.Name;
        }

        private List<long> ParseItemIdsFromSearchResults(List<string> searchResults)
        {
            return searchResults
                .Select(str => long.Parse(str.Split('|')[0].Replace("ID:", "")))
                .ToList();
        }

        private float ExtractScoreFromSearchResult(List<string> searchResults, long itemId)
        {
            var itemSearchStr = searchResults.FirstOrDefault(s => s.StartsWith($"ID:{itemId}|"));
            if (itemSearchStr == null)
                return 0;

            var scorePart = itemSearchStr.Split('|').Last();
            return float.Parse(scorePart.Replace("Score:", ""));
        }

        private QDrantSearchModels MapItemToSearchModel(Item item, List<string> searchResults)
        {
            var itemModel = _mapper.Map<ItemModel>(item);

            return new QDrantSearchModels
            {
                Id = item.Id,
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
                Score = ExtractScoreFromSearchResult(searchResults, item.Id)
            };
        }

        public async Task<BaseResponseModel> OutfitSuggestion(long userId, long? occasionId, string? weather = null)
        {
            var overallStopwatch = Stopwatch.StartNew();

            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);

            // Get occasion and user characteristics
            var occasionString = await GetOccasionStringAsync(occasionId);
            var userCharacteristic = await _userService.GetUserCharacteristic(userId);
            var characteristicString = BuildUserCharacteristicString(userCharacteristic);

            // Get outfit suggestions from Gemini
            var geminiStopwatch = Stopwatch.StartNew();
            var itemDescriptions = await _geminiService.OutfitSuggestion(occasionString, characteristicString, weather);
            geminiStopwatch.Stop();
            Console.WriteLine($"[TIMING] Gemini outfit suggestion: {geminiStopwatch.ElapsedMilliseconds}ms");

            // Search for matching items in user's wardrobe
            var searchStopwatch = Stopwatch.StartNew();
            var searchResults = await _qdrantService.SearchItemIdsByUserId(itemDescriptions, userId);
            searchStopwatch.Stop();
            Console.WriteLine($"[TIMING] Qdrant search: {searchStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"[DEBUG] Search results count: {searchResults.Count}");

            // Let Gemini choose the best outfit combination directly from search results
            var outfitSelection = await _geminiService.ChooseOutfit(occasionString, characteristicString, searchResults, weather);
            Console.WriteLine($"[DEBUG] Gemini selected {outfitSelection.ItemIds?.Count ?? 0} items");

            // Fetch final selected items
            var selectedItems = outfitSelection.ItemIds?.Any() == true
                ? await _unitOfWork.ItemRepository.GetItemsByIdsAsync(outfitSelection.ItemIds)
                : new List<Item>();

            var selectedItemModels = selectedItems.Select(item => _mapper.Map<ItemModel>(item)).ToList();

            overallStopwatch.Stop();
            Console.WriteLine($"[TIMING] *** TOTAL: {overallStopwatch.ElapsedMilliseconds}ms ***");

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_SUGGESTION_SUCCESS,
                Data = new OutfitSuggestionModel
                {
                    SuggestedItems = selectedItemModels,
                    Reason = outfitSelection.Reason
                }
            };
        }

        public async Task<BaseResponseModel> VirtualTryOn(IFormFile human, List<string> itemURLs)
        {
            if (human == null || human.Length == 0)
            {
                throw new BadRequestException(MessageConstants.IMAGE_IS_NOT_VALID);
            }

            if (itemURLs == null || itemURLs.Count == 0 || itemURLs.Count > 3)
            {
                throw new BadRequestException(MessageConstants.ITEM_URLS_NOT_VALID);
            }

            var client = _httpClientFactory.CreateClient("SplitItem");
            var requestUrl = "be/api/v1/virtual-tryon/try-on";

            using var formData = new MultipartFormDataContent();

            // Add human image file
            await using var fileStream = human.OpenReadStream();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(human.ContentType);
            formData.Add(fileContent, "human_image", human.FileName);

            // Add clothing URLs as a single comma-separated string
            var clothingUrlsString = string.Join(",", itemURLs);
            formData.Add(new StringContent(clothingUrlsString), "clothing_urls");

            var response = await client.PostAsync(requestUrl, formData);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new BadRequestException($"Virtual try-on failed: {errorContent}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var tryOnResult = JsonSerializer.Deserialize<VirtualTryOnResponse>(responseBody, serializerOptions);

            if (tryOnResult == null || string.IsNullOrEmpty(tryOnResult.Url))
            {
                throw new BadRequestException("Virtual try-on service returned invalid response");
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.VIRTUAL_TRY_ON_SUCCESS,
                Data = new
                {
                    Time = tryOnResult.Time,
                    Url = tryOnResult.Url
                }
            };
        }

        public async Task<BaseResponseModel> OutfitSuggestionV2(OutfitSuggestionRequestModel model)
        {
            var overallStopwatch = Stopwatch.StartNew();

            // Step 1: Validate user exists
            await ValidateUserForOutfitSuggestion(model.UserId);

            // Step 2: Fetch all required data in PARALLEL
            var (occasionString, userCharacteristic, listSeason, listOccasion, listStyle) = 
                await FetchRequiredDataForOutfitSuggestion(model);

            // Step 3: Build characteristic string and get AI suggestions
            var characteristicString = BuildUserCharacteristicString(userCharacteristic);
            var (selectedSeasonIds, selectedOccasionIds, selectedStyleIds) = 
                await GetAISuggestionsForCategories(occasionString, characteristicString, listSeason, listOccasion, listStyle);

            // Step 4: Filter and map items
            var combinedItems = await FilterAndMapItemsForOutfitSuggestion(selectedSeasonIds, selectedOccasionIds, selectedStyleIds, model.UserId);

            // Step 5: Generate multiple outfit suggestions
            var totalOutfits = model.TotalOutfit > 0 ? model.TotalOutfit : 1;
            var outfitSelections = await GenerateMultipleOutfitSuggestions(combinedItems, occasionString, characteristicString, model.Weather, totalOutfits);

            // Step 6: Map selected items from all outfit selections
            var allSuggestedOutfits = await MapOutfitSelectionsToSuggestions(outfitSelections);

            overallStopwatch.Stop();
            Console.WriteLine($"[TIMING] *** TOTAL: {overallStopwatch.ElapsedMilliseconds}ms ***");

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_SUGGESTION_SUCCESS,
                Data = allSuggestedOutfits
            };
        }
    }
}
