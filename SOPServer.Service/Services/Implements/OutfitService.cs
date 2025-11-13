using AutoMapper;
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
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System.Text;

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

            var outfitCalendars = await _unitOfWork.OutfitRepository.GetOutfitCalendarPaginationAsync(
                paginationParameter,
                filter: x => x.UserId == userId &&
                           x.UserOccasion != null &&
                           (!filterStartDate.HasValue || x.UserOccasion.DateOccasion >= filterStartDate.Value) &&
                           (!filterEndDate.HasValue || x.UserOccasion.DateOccasion <= filterEndDate.Value),
                orderBy: q => q.OrderBy(x => x.UserOccasion != null ? x.UserOccasion.DateOccasion : DateTime.MaxValue));

            var models = _mapper.Map<Pagination<OutfitCalendarModel>>(outfitCalendars);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_OUTFIT_CALENDAR_SUCCESS,
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
            long? userOccasionId = model.UserOccasionId;

            if (model.IsDaily)
            {
                // Validate that Time is provided for daily outfits
                if (!model.Time.HasValue)
                {
                    throw new BadRequestException("Time is required when IsDaily is true");
                }

                // Look for existing "Daily" UserOccasion for this user
                var dailyOccasion = _unitOfWork.UserOccasionRepository.GetQueryable()
                    .FirstOrDefault(uo => uo.UserId == userId && uo.Name == "Daily");

                if (dailyOccasion == null)
                {
                    // Create a new "Daily" UserOccasion
                    dailyOccasion = new UserOccasion
                    {
                        UserId = userId,
                        Name = "Daily",
                        Description = "Daily outfit schedule",
                        DateOccasion = DateTime.Today,
                        StartTime = DateTime.Today.Add(model.Time.Value),
                        EndTime = model.EndTime.HasValue ? DateTime.Today.Add(model.EndTime.Value) : null,
                        OccasionId = null
                    };
                    await _unitOfWork.UserOccasionRepository.AddAsync(dailyOccasion);
                    await _unitOfWork.SaveAsync();
                }
                else
                {
                    // Update the StartTime and EndTime if different
                    var newStartTime = DateTime.Today.Add(model.Time.Value);
                    var newEndTime = model.EndTime.HasValue ? DateTime.Today.Add(model.EndTime.Value) : (DateTime?)null;

                    if (dailyOccasion.StartTime != newStartTime || dailyOccasion.EndTime != newEndTime)
                    {
                        dailyOccasion.StartTime = newStartTime;
                        dailyOccasion.EndTime = newEndTime;
                        dailyOccasion.DateOccasion = DateTime.Today;
                        _unitOfWork.UserOccasionRepository.UpdateAsync(dailyOccasion);
                        await _unitOfWork.SaveAsync();
                    }
                }

                userOccasionId = dailyOccasion.Id;
            }
            else if (model.UserOccasionId.HasValue)
            {
                // Verify user occasion if provided
                var userOccasion = await _unitOfWork.UserOccasionRepository.GetByIdAsync(model.UserOccasionId.Value);
                if (userOccasion == null)
                {
                    throw new NotFoundException(MessageConstants.USER_OCCASION_NOT_FOUND);
                }

                if (userOccasion.UserId != userId)
                {
                    throw new ForbiddenException(MessageConstants.USER_OCCASION_ACCESS_DENIED);
                }
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
                // Validate that Time is provided for daily outfits
                if (!model.Time.HasValue)
                {
                    throw new BadRequestException("Time is required when IsDaily is true");
                }

                // Look for existing "Daily" UserOccasion for this user
                var dailyOccasion = _unitOfWork.UserOccasionRepository.GetQueryable()
                    .FirstOrDefault(uo => uo.UserId == userId && uo.Name == "Daily");

                if (dailyOccasion == null)
                {
                    // Create a new "Daily" UserOccasion
                    dailyOccasion = new UserOccasion
                    {
                        UserId = userId,
                        Name = "Daily",
                        Description = "Daily outfit schedule",
                        DateOccasion = DateTime.Today,
                        StartTime = DateTime.Today.Add(model.Time.Value),
                        EndTime = model.EndTime.HasValue ? DateTime.Today.Add(model.EndTime.Value) : null,
                        OccasionId = null
                    };
                    await _unitOfWork.UserOccasionRepository.AddAsync(dailyOccasion);
                    await _unitOfWork.SaveAsync();
                }
                else
                {
                    // Update the StartTime and EndTime if different
                    var newStartTime = DateTime.Today.Add(model.Time.Value);
                    var newEndTime = model.EndTime.HasValue ? DateTime.Today.Add(model.EndTime.Value) : (DateTime?)null;

                    if (dailyOccasion.StartTime != newStartTime || dailyOccasion.EndTime != newEndTime)
                    {
                        dailyOccasion.StartTime = newStartTime;
                        dailyOccasion.EndTime = newEndTime;
                        dailyOccasion.DateOccasion = DateTime.Today;
                        _unitOfWork.UserOccasionRepository.UpdateAsync(dailyOccasion);
                        await _unitOfWork.SaveAsync();
                    }
                }

                // Set the UserOccasionId to the Daily occasion
                outfitCalendar.UserOccassionId = dailyOccasion.Id;
            }
            else if (model.UserOccasionId.HasValue)
            {
                // Verify user occasion if provided
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

        public async Task<BaseResponseModel> OutfitSuggestion(long userId, long? userOccasionId)
        {
            var userCharacteristic = await _userService.GetUserCharacteristic(userId);

            // Build comprehensive user characteristic string
            string userCharacteristicString = BuildUserCharacteristicString(userCharacteristic);

            // Build comprehensive occasion string if provided
            string occasionString = string.Empty;
            if (userOccasionId.HasValue)
            {
                var userOccasion = await _unitOfWork.UserOccasionRepository.GetByIdIncludeAsync(
                    userOccasionId.Value, 
                    include: x => x.Include(x => x.Occasion));
                
                if (userOccasion != null)
                {
                    occasionString = BuildOccasionString(userOccasion);
                }
            }

            // Step 1: Generate outfit item descriptions from Gemini
            var itemDescriptions = await _geminiService.GenerateOutfitSuggestItem(userCharacteristicString, occasionString);
            
            if (itemDescriptions == null || !itemDescriptions.Any())
            {
                throw new BadRequestException(MessageConstants.OUTFIT_SUGGESTION_FAILED);
            }

            // Step 2: For each description, search for matching items in user's wardrobe
            var matchedItemsDict = new Dictionary<string, List<(long itemId, float score)>>
            ();
            
            foreach (var description in itemDescriptions)
            {
                var embedding = await _geminiService.EmbeddingText(description);
                if (embedding == null || !embedding.Any())
                    continue;

                var searchResults = await _qdrantService.SearchSimilarityByUserId(embedding, userId);
                
                if (searchResults != null && searchResults.Any())
                {
                    matchedItemsDict[description] = searchResults
                        .Select(r => ((long)r.id, r.score))
                        .ToList();
                }
            }

            // Step 3: Collect all unique matched items
            var allMatchedItemIds = matchedItemsDict.Values
                .SelectMany(matches => matches.Select(m => m.itemId))
                .Distinct()
                .ToList();

            if (!allMatchedItemIds.Any())
            {
                throw new NotFoundException(MessageConstants.NO_MATCHING_ITEMS_FOUND);
            }

            // Step 4: Get full item details for all matched items
            var matchedItems = new List<ItemModel>();
            foreach (var itemId in allMatchedItemIds)
            {
                var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(
                    itemId,
                    include: query => query
                        .Include(x => x.Category).ThenInclude(c => c.Parent)
                        .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                        .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                        .Include(x => x.ItemStyles).ThenInclude(x => x.Style));

                if (item != null && item.UserId == userId)
                {
                    matchedItems.Add(_mapper.Map<ItemModel>(item));
                }
            }

            if (!matchedItems.Any())
            {
                throw new NotFoundException(MessageConstants.NO_MATCHING_ITEMS_FOUND);
            }

            // Step 5: Let Gemini select the best combination from matched items
            var outfitSelection = await _geminiService.SelectOutfitFromItems(
                userCharacteristicString, 
                occasionString, 
                matchedItems);

            if (outfitSelection == null || outfitSelection.ItemIds == null || !outfitSelection.ItemIds.Any())
            {
                throw new BadRequestException(MessageConstants.OUTFIT_SUGGESTION_FAILED);
            }

            // Step 6: Build the response with selected items and their match scores
            var selectedItems = new List<SuggestedItemModel>();
            foreach (var itemId in outfitSelection.ItemIds)
            {
                var item = matchedItems.FirstOrDefault(i => i.Id == itemId);
                if (item != null)
                {
                    // Find the best match score for this item across all descriptions
                    var bestScore = matchedItemsDict.Values
                        .SelectMany(matches => matches)
                        .Where(m => m.itemId == itemId)
                        .Select(m => m.score)
                        .DefaultIfEmpty(0f)
                        .Max();

                    selectedItems.Add(new SuggestedItemModel
                    {
                        ItemId = item.Id,
                        Name = item.Name,
                        CategoryName = item.CategoryName,
                        Color = item.Color,
                        ImgUrl = item.ImgUrl,
                        AiDescription = item.AiDescription ?? item.Name,
                        MatchScore = bestScore
                    });
                }
            }

            var suggestionResponse = new OutfitSuggestionModel
            {
                SuggestedItems = selectedItems,
                Reason = outfitSelection.Reason
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OUTFIT_SUGGESTION_SUCCESS,
                Data = suggestionResponse
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
    }
}
