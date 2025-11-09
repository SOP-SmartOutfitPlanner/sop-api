using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.OutfitCalendarModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                           (!filterStartDate.HasValue || x.DateUsed >= filterStartDate.Value) &&
                           (!filterEndDate.HasValue || x.DateUsed <= filterEndDate.Value),
                orderBy: q => q.OrderBy(x => x.DateUsed));

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

            // Verify outfit exists and belongs to user
            var outfit = await _unitOfWork.OutfitRepository.GetByIdAsync(model.OutfitId);
            if (outfit == null)
            {
                throw new NotFoundException(MessageConstants.OUTFIT_NOT_FOUND);
            }

            if (outfit.UserId != userId)
            {
                throw new ForbiddenException(MessageConstants.OUTFIT_ACCESS_DENIED);
            }

            // Verify user occasion if provided
            if (model.UserOccasionId.HasValue)
            {
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

            var outfitCalendar = _mapper.Map<OutfitUsageHistory>(model);
            outfitCalendar.UserId = userId;
            outfitCalendar.CreatedBy = OutfitCreatedBy.USER;

            await _unitOfWork.OutfitRepository.AddOutfitCalendarAsync(outfitCalendar);
            await _unitOfWork.SaveAsync();

            var created = await _unitOfWork.OutfitRepository.GetOutfitCalendarByIdAsync(outfitCalendar.Id);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.OUTFIT_CALENDAR_CREATE_SUCCESS,
                Data = _mapper.Map<OutfitCalendarModel>(created)
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

            // Verify user occasion if provided
            if (model.UserOccasionId.HasValue)
            {
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

            if (model.DateUsed.HasValue)
            {
                outfitCalendar.DateUsed = model.DateUsed.Value;
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
            // Step 1: Get user characteristics
            var userCharacteristic = await _userService.GetUserCharacteristic(userId);
            
            if (userCharacteristic == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Step 2: Get occasion and weather information if provided
            string occasionName = "casual";
            string weatherInfo = "moderate";
            
            if (userOccasionId.HasValue)
            {
                var userOccasion = await _unitOfWork.UserOccasionRepository.GetByIdIncludeAsync(
                    userOccasionId.Value,
                    include: query => query.Include(uo => uo.Occasion));
                
                if (userOccasion != null)
                {
                    occasionName = userOccasion.Occasion?.Name ?? userOccasion.Name ?? "casual";
                    weatherInfo = userOccasion.WeatherSnapshot ?? "moderate";
                }
            }

            // Step 3: Build user context for Gemini
            var userContext = BuildUserContext(userCharacteristic, occasionName, weatherInfo);

            // Step 4: Generate outfit description using Gemini
            var outfitGeneration = await _geminiService.GenerateOutfitDescription(userContext);
            
            if (outfitGeneration == null || outfitGeneration.Items == null || !outfitGeneration.Items.Any())
            {
                throw new BadRequestException("Failed to generate outfit description");
            }

            // Step 5: Determine category parent IDs based on outfit type
            var categoryMapping = new Dictionary<string, long>
            {
                { "Top", 1 },
                { "Bottom", 2 },
                { "Shoe", 3 },
                { "Accessory", 4 },
                { "FullBody", 41 }
            };

            // Step 6: Search for matching items from user's wardrobe using Qdrant
            var candidatesByCategory = new List<CategoryItemCandidates>();
            
            foreach (var itemDesc in outfitGeneration.Items)
            {
                if (!categoryMapping.TryGetValue(itemDesc.Category, out long categoryId))
                {
                    continue;
                }

                // Create embedding for the item description
                var embedding = await _geminiService.EmbeddingText(itemDesc.Description);
                
                if (embedding == null || !embedding.Any())
                {
                    continue;
                }

                // Search for similar items in Qdrant
                var searchResults = await _qdrantService.SearchSimilarityByUserIdAndCategoryId(
                    embedding, userId, categoryId);

                if (searchResults == null || !searchResults.Any())
                {
                    continue;
                }

                // Retrieve item details from database
                var itemIds = searchResults.Select(r => (long)r.id).ToList();
                var items = await _unitOfWork.ItemRepository.GetQueryable()
                    .Where(i => itemIds.Contains(i.Id))
                    .Include(i => i.Category)
                    .ToListAsync();

                if (items == null || !items.Any())
                {
                    continue;
                }

                // Convert to candidate models
                var candidates = items.Select(item => new ItemCandidateModel
                {
                    Id = item.Id,
                    Name = item.Name ?? "",
                    Description = item.AiDescription ?? "",
                    Color = item.Color ?? "",
                    Pattern = item.Pattern ?? "",
                    Fabric = item.Fabric ?? ""
                }).ToList();

                candidatesByCategory.Add(new CategoryItemCandidates
                {
                    Category = itemDesc.Category,
                    Items = candidates
                });
            }

            // Step 7: If no items found, return error
            if (!candidatesByCategory.Any())
            {
                throw new NotFoundException("No matching items found in your wardrobe for this outfit");
            }

            // Step 8: Use Gemini to select the best outfit combination
            var selection = await _geminiService.SelectBestOutfit(userContext, candidatesByCategory);
            
            if (selection == null || selection.SelectedItemIds == null || !selection.SelectedItemIds.Any())
            {
                throw new BadRequestException("Failed to select best outfit combination");
            }

            // Step 9: Retrieve final selected items
            var selectedItems = await _unitOfWork.ItemRepository.GetQueryable()
                .Where(i => selection.SelectedItemIds.Contains(i.Id))
                .Include(i => i.Category)
                .ToListAsync();

            if (selectedItems == null || !selectedItems.Any())
            {
                throw new NotFoundException("Selected items not found");
            }

            // Step 10: Build response
            var response = new OutfitSuggestionResponseModel
            {
                SelectedOutfit = new SelectedOutfitModel
                {
                    Items = selectedItems.Select(item => new OutfitItemDetailModel
                    {
                        Id = item.Id,
                        Name = item.Name ?? "",
                        Category = item.Category?.Name ?? "",
                        ImgUrl = item.ImgUrl ?? ""
                    }).ToList()
                },
                Explanation = selection.Explanation ?? "This outfit was selected based on your style preferences and the occasion."
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "Outfit suggestion generated successfully",
                Data = response
            };
        }

        private string BuildUserContext(UserCharacteristicModel user, string occasion, string weather)
        {
            var stylesText = user.Styles != null && user.Styles.Any() 
                ? string.Join(", ", user.Styles) 
                : "casual";
            
            var colorsText = user.PreferedColor != null && user.PreferedColor.Any() 
                ? string.Join(", ", user.PreferedColor) 
                : "neutral tones";

            var age = user.Dob.HasValue 
                ? DateTime.Today.Year - user.Dob.Value.Year 
                : 25;

            return $@"User Profile:
- Gender: {user.Gender}
- Preferred Styles: {stylesText}
- Favorite Colors: {colorsText}
- Profession: {user.Job ?? "not specified"}
- Current Weather: {weather}
- Current Occasion: {occasion}";
        }
    }
}
