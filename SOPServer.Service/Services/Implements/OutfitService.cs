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
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;

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
            // Step 1: Retrieve user characteristics
            var userCharacteristic = await _userService.GetUserCharacteristic(userId);
            
            if (userCharacteristic == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Step 2: Build context object with user and occasion details
            var context = new OutfitGenerationContextModel
            {
                Gender = userCharacteristic.Gender.ToString(),
                Age = userCharacteristic.Dob.HasValue 
                    ? DateTime.Now.Year - userCharacteristic.Dob.Value.Year 
                    : null,
                Profession = userCharacteristic.Job,
                PreferredStyles = userCharacteristic.Styles,
                FavoriteColors = userCharacteristic.PreferedColor,
                AvoidedColors = userCharacteristic.AvoidedColor,
                Location = userCharacteristic.Location
            };

            // If userOccasionId is provided, fetch occasion details
            if (userOccasionId.HasValue)
            {
                var userOccasion = await _unitOfWork.UserOccasionRepository.GetByIdIncludeAsync(
                    userOccasionId.Value,
                    include: query => query.Include(uo => uo.Occasion));

                if (userOccasion == null)
                {
                    throw new NotFoundException(MessageConstants.USER_OCCASION_NOT_FOUND);
                }

                if (userOccasion.UserId != userId)
                {
                    throw new ForbiddenException(MessageConstants.USER_OCCASION_ACCESS_DENIED);
                }

                context.OccasionName = userOccasion.Occasion?.Name ?? userOccasion.Name;
                context.OccasionDescription = userOccasion.Description;
                context.WeatherSnapshot = userOccasion.WeatherSnapshot;
            }

            // Step 3: Ask Gemini to generate outfit description
            var outfitGeneration = await _geminiService.GenerateOutfitDescription(context);
            
            if (outfitGeneration == null)
            {
                throw new BadRequestException("Failed to generate outfit description");
            }

            // Step 4: Create embeddings and search Qdrant for similar items
            var shortlistedItems = new ShortlistedItemsModel();

            if (outfitGeneration.OutfitType == "Full-Body" && outfitGeneration.FullBody != null)
            {
                // Generate embedding for full-body item
                var fullBodyDescription = BuildItemDescription(outfitGeneration.FullBody);
                var fullBodyEmbedding = await _geminiService.EmbeddingText(fullBodyDescription);
                
                if (fullBodyEmbedding != null)
                {
                    // Search for similar items in parent category 41 (Full-Body)
                    var searchResults = await _qdrantService.SearchSimilarityByUserIdAndParentCategory(
                        fullBodyEmbedding, userId, 41);
                    
                    shortlistedItems.FullBodyItems = await GetItemDetailsFromSearchResults(searchResults);
                }
            }
            else if (outfitGeneration.OutfitType == "Separated")
            {
                // Generate embeddings for top and bottom
                if (outfitGeneration.Top != null)
                {
                    var topDescription = BuildItemDescription(outfitGeneration.Top);
                    var topEmbedding = await _geminiService.EmbeddingText(topDescription);
                    
                    if (topEmbedding != null)
                    {
                        // Search for similar items in parent category 1 (Top)
                        var searchResults = await _qdrantService.SearchSimilarityByUserIdAndParentCategory(
                            topEmbedding, userId, 1);
                        
                        shortlistedItems.TopItems = await GetItemDetailsFromSearchResults(searchResults);
                    }
                }

                if (outfitGeneration.Bottom != null)
                {
                    var bottomDescription = BuildItemDescription(outfitGeneration.Bottom);
                    var bottomEmbedding = await _geminiService.EmbeddingText(bottomDescription);
                    
                    if (bottomEmbedding != null)
                    {
                        // Search for similar items in parent category 2 (Bottom)
                        var searchResults = await _qdrantService.SearchSimilarityByUserIdAndParentCategory(
                            bottomEmbedding, userId, 2);
                        
                        shortlistedItems.BottomItems = await GetItemDetailsFromSearchResults(searchResults);
                    }
                }
            }

            // Validate that we have shortlisted items
            var hasFullBodyItems = shortlistedItems.FullBodyItems != null && shortlistedItems.FullBodyItems.Count > 0;
            var hasTopBottomItems = (shortlistedItems.TopItems != null && shortlistedItems.TopItems.Count > 0) ||
                                   (shortlistedItems.BottomItems != null && shortlistedItems.BottomItems.Count > 0);

            if (!hasFullBodyItems && !hasTopBottomItems)
            {
                throw new NotFoundException("No matching items found in your wardrobe for this occasion");
            }

            // Step 5: Send shortlisted items to Gemini for final selection
            var finalSelection = await _geminiService.SelectBestOutfit(context, shortlistedItems);
            
            if (finalSelection == null || finalSelection.SelectedOutfit == null)
            {
                throw new BadRequestException("Failed to select best outfit");
            }

            // Step 6: Build response with selected items
            var response = new OutfitSuggestionResponseModel
            {
                Explanation = finalSelection.Explanation,
                SelectedOutfit = new SelectedOutfitModel()
            };

            // Get the selected items from the database
            if (finalSelection.SelectedOutfit.FullBodyId.HasValue)
            {
                var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(
                    finalSelection.SelectedOutfit.FullBodyId.Value,
                    include: query => query.Include(i => i.Category));
                
                if (item != null)
                {
                    response.SelectedOutfit.FullBody = new OutfitItemSuggestionModel
                    {
                        Id = item.Id,
                        Name = item.Name,
                        CategoryName = item.Category?.Name ?? "Unknown",
                        ImgUrl = item.ImgUrl
                    };
                }
            }

            if (finalSelection.SelectedOutfit.TopId.HasValue)
            {
                var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(
                    finalSelection.SelectedOutfit.TopId.Value,
                    include: query => query.Include(i => i.Category));
                
                if (item != null)
                {
                    response.SelectedOutfit.Top = new OutfitItemSuggestionModel
                    {
                        Id = item.Id,
                        Name = item.Name,
                        CategoryName = item.Category?.Name ?? "Unknown",
                        ImgUrl = item.ImgUrl
                    };
                }
            }

            if (finalSelection.SelectedOutfit.BottomId.HasValue)
            {
                var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(
                    finalSelection.SelectedOutfit.BottomId.Value,
                    include: query => query.Include(i => i.Category));
                
                if (item != null)
                {
                    response.SelectedOutfit.Bottom = new OutfitItemSuggestionModel
                    {
                        Id = item.Id,
                        Name = item.Name,
                        CategoryName = item.Category?.Name ?? "Unknown",
                        ImgUrl = item.ImgUrl
                    };
                }
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = "Outfit suggestion generated successfully",
                Data = response
            };
        }

        private string BuildItemDescription(GeminiItemDescriptionModel itemDescription)
        {
            var parts = new List<string>();
            
            parts.Add($"Type: {itemDescription.Type}");
            
            if (itemDescription.ColorPalette != null && itemDescription.ColorPalette.Count > 0)
                parts.Add($"Colors: {string.Join(", ", itemDescription.ColorPalette)}");
            
            if (itemDescription.Style != null && itemDescription.Style.Count > 0)
                parts.Add($"Style: {string.Join(", ", itemDescription.Style)}");
            
            if (itemDescription.Occasion != null && itemDescription.Occasion.Count > 0)
                parts.Add($"Occasion: {string.Join(", ", itemDescription.Occasion)}");
            
            if (itemDescription.Season != null && itemDescription.Season.Count > 0)
                parts.Add($"Season: {string.Join(", ", itemDescription.Season)}");
            
            return string.Join(". ", parts);
        }

        private async Task<List<ShortlistedItemModel>> GetItemDetailsFromSearchResults(List<BusinessModels.QDrantModels.QDrantSearchModels> searchResults)
        {
            var items = new List<ShortlistedItemModel>();

            foreach (var result in searchResults)
            {
                var item = await _unitOfWork.ItemRepository.GetByIdIncludeAsync(
                    result.id,
                    include: query => query
                        .Include(i => i.Category)
                        .Include(i => i.ItemStyles)
                            .ThenInclude(ist => ist.Style)
                        .Include(i => i.ItemOccasions)
                            .ThenInclude(io => io.Occasion)
                        .Include(i => i.ItemSeasons)
                            .ThenInclude(ise => ise.Season));

                if (item != null)
                {
                    items.Add(new ShortlistedItemModel
                    {
                        Id = item.Id,
                        Name = item.Name,
                        CategoryName = item.Category?.Name ?? "Unknown",
                        ImgUrl = item.ImgUrl,
                        Color = item.Color,
                        AiDescription = item.AiDescription,
                        Pattern = item.Pattern,
                        Fabric = item.Fabric,
                        Styles = item.ItemStyles?.Select(ist => ist.Style?.Name).Where(n => n != null).ToList(),
                        Occasions = item.ItemOccasions?.Select(io => io.Occasion?.Name).Where(n => n != null).ToList(),
                        Seasons = item.ItemSeasons?.Select(ise => ise.Season?.Name).Where(n => n != null).ToList()
                    });
                }
            }

            return items;
        }
    }
}
