using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.API.Attributes;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.OutfitCalendarModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.BusinessModels.VirtualTryOnModels;
using SOPServer.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    /// <summary>
    /// Outfit management endpoints for creating, viewing, updating, and managing user outfits
    /// </summary>
    [Route("api/v1/outfits")]
    [ApiController]
    //[Authorize(Roles = "USER,STYLIST,ADMIN")]
    public class OutfitController : BaseController
    {
        private readonly IOutfitService _outfitService;

        public OutfitController(IOutfitService outfitService)
        {
            _outfitService = outfitService;
        }

        /// <summary>
        /// Get all outfits with pagination and search
        /// </summary>
        /// <remarks>
        /// **Roles:** ADMIN only
        ///
        /// **Query Parameters:**
        /// - `page-index`: Page number (default: 1)
        /// - `page-size`: Items per page (default: 10)
        /// - `search`: Search in name or description (optional)
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> GetAllOutfits([FromQuery] PaginationParameter paginationParameter)
        {
            return ValidateAndExecute(async () => await _outfitService.GetAllOutfitPaginationAsync(paginationParameter));
        }

        /// <summary>
        /// Get user's outfits with pagination, search, and filters
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Query Parameters:**
        /// - `page-index`: Page number (default: 1)
        /// - `page-size`: Items per page (default: 10)
        /// - `search`: Search in name or description (optional)
        /// - `is-favorite`: Filter by favorite status (optional)
        /// - `is-saved`: Filter by saved status (optional)
        /// </remarks>
        [HttpGet("user")]
        public Task<IActionResult> GetOutfitsByUser(
            [FromQuery] PaginationParameter paginationParameter,
            [FromQuery(Name = "is-favorite")] bool? isFavorite,
            [FromQuery(Name = "is-saved")] bool? isSaved,
            [FromQuery(Name = "start-date")] DateTime? startDate,
            [FromQuery(Name = "end-date")] DateTime? endDate)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.GetOutfitByUserPaginationAsync(paginationParameter, userId, isFavorite, isSaved, startDate, endDate));
        }

        /// <summary>
        /// Get outfit by ID with item details
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Note:** Users can only access their own outfits
        /// </remarks>
        [HttpGet("{id}")]
        public Task<IActionResult> GetOutfitById(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.GetOutfitByIdAsync(id, userId));
        }

        /// <summary>
        /// Create a new outfit
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Request Body:**
        /// - `name`: Outfit name (optional)
        /// - `description`: Outfit description (optional)
        /// - `itemIds`: Array of item IDs (optional, prevents duplicates)
        ///
        /// **Note:** UserId is extracted from JWT token automatically
        /// **Note:** Subject to subscription limits based on user's plan
        /// </remarks>
        [HttpPost]
        // TODO: Add FeatureCode for outfits or remove subscription limit
        // [CheckSubscriptionLimit("outfitsCreated", "maxOutfits")]
        public Task<IActionResult> CreateOutfit([FromBody] OutfitCreateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.CreateOutfitAsync(userId, model));
        }

        /// <summary>
        /// Update outfit name and description
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Request Body:**
        /// - `name`: New outfit name (optional)
        /// - `description`: New outfit description (optional)
        ///
        /// **Note:** Users can only update their own outfits
        /// </remarks>
        [HttpPut("{id}")]
        public Task<IActionResult> UpdateOutfit(long id, [FromBody] OutfitUpdateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.UpdateOutfitAsync(id, userId, model));
        }

        /// <summary>
        /// Delete outfit (soft delete)
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Note:** Users can only delete their own outfits
        /// </remarks>
        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteOutfit(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.DeleteOutfitAsync(id, userId));
        }

        /// <summary>
        /// Toggle outfit favorite status
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        /// </remarks>
        [HttpPut("{id}/favorite")]
        public Task<IActionResult> ToggleOutfitFavorite(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.ToggleOutfitFavoriteAsync(id, userId));
        }

        /// <summary>
        /// Toggle outfit saved status
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        /// </remarks>
        [HttpPut("{id}/save")]
        public Task<IActionResult> ToggleOutfitSave(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.ToggleOutfitSaveAsync(id, userId));
        }


        /// <summary>
        /// Get user's outfit calendar entries with pagination and filters
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Query Parameters:**
        /// - `page-index`: Page number (default: 1)
        /// - `page-size`: Items per page (default: 10)
        /// - `start-date`: Filter entries from this date (optional, format: yyyy-MM-dd)
        /// - `end-date`: Filter entries until this date (optional, format: yyyy-MM-dd)
        /// - `year`: Filter entries by year (optional)
        /// - `month`: Filter entries by month (1-12, requires year parameter, optional)
        ///
        /// **Note:** Users can only see their own outfit calendar entries
        ///
        /// **Examples:**
        /// - Get current week: ?filter-type=0
        /// - Get current month: ?filter-type=1
        /// - Get specific month: ?filter-type=2&amp;year=2025&amp;month=12
        /// - Get date range: ?filter-type=3&amp;start-date=2025-12-01&amp;end-date=2025-12-31
        /// - Legacy: ?year=2025&amp;month=12 (backward compatible)
        /// </remarks>
        [HttpGet("calendar")]
        public Task<IActionResult> GetOutfitCalendar(
            [FromQuery] PaginationParameter paginationParameter,
            [FromQuery(Name = "filter-type")] CalendarFilterType? filterType,
            [FromQuery(Name = "start-date")] DateTime? startDate,
            [FromQuery(Name = "end-date")] DateTime? endDate,
            [FromQuery] int? year,
            [FromQuery] int? month)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.GetOutfitCalendarPaginationAsync(
                paginationParameter, userId, filterType, startDate, endDate, year, month));
        }

        /// <summary>
        /// Get outfit calendar entry by ID with full details
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Note:** Users can only access their own outfit calendar entries
        /// </remarks>
        [HttpGet("calendar/{id}")]
        public Task<IActionResult> GetOutfitCalendarById(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.GetOutfitCalendarByIdAsync(id, userId));
        }

        /// <summary>
        /// Get all outfit calendar entries for a specific user occasion
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Note:**
        /// - Users can only access outfit calendars for their own occasions
        /// - Returns all outfits scheduled for the specified occasion
        /// - Includes full outfit and item details
        /// </remarks>
        [HttpGet("calendar/occasion/{userOccasionId}")]
        public Task<IActionResult> GetOutfitCalendarByUserOccasionId(long userOccasionId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.GetOutfitCalendarByUserOccasionIdAsync(userOccasionId, userId));
        }

        /// <summary>
        /// Create a new outfit calendar entry (schedule an outfit for a date)
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Request Body:**
        /// - `outfitId`: ID of the outfit to schedule (required)
        /// - `userOccasionId`: Link to a specific occasion/event (optional)
        /// - `dateUsed`: Date when the outfit will be worn (required)
        ///
        /// **Note:** UserId is extracted from JWT token automatically
        /// </remarks>
        [HttpPost("calendar")]
        public Task<IActionResult> CreateOutfitCalendar([FromBody] OutfitCalendarCreateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.CreateOutfitCalendarAsync(userId, model));
        }

        /// <summary>
        /// Update outfit calendar entry
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Request Body:** All fields are optional, only provided fields will be updated
        /// - `outfitId`: ID of the outfit to schedule
        /// - `userOccasionId`: Link to a specific occasion/event
        /// - `dateUsed`: Date when the outfit will be worn
        ///
        /// **Note:** Users can only update their own outfit calendar entries
        /// </remarks>
        [HttpPut("calendar/{id}")]
        public Task<IActionResult> UpdateOutfitCalendar(long id, [FromBody] OutfitCalendarUpdateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.UpdateOutfitCalendarAsync(id, userId, model));
        }

        /// <summary>
        /// Delete outfit calendar entry (soft delete)
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Note:** Users can only delete their own outfit calendar entries
        /// </remarks>
        [HttpDelete("calendar/{id}")]
        public Task<IActionResult> DeleteOutfitCalendar(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.DeleteOutfitCalendarAsync(id, userId));
        }

        /// <summary>
        /// Generate AI-powered outfit suggestions for user
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Query Parameters:**
        /// - `userId`: User ID (required)
        /// - `occasionId`: Occasion ID (optional)
        /// - `weather`: Weather information for outfit suggestions (optional, e.g., "sunny, 25°C" or "rainy, 15°C")
        ///
        /// **Note:** Subject to subscription limits based on user's plan (monthly reset)
        /// </remarks>
        [HttpGet("suggestion")]
        [CheckSubscriptionLimit(FeatureCode.OutfitSuggestion)]
        public Task<IActionResult> OutfitSuggestion(long userId, long? occasionId, string? weather = null)
        {
            return ValidateAndExecute(async () => await _outfitService.OutfitSuggestion(userId, occasionId, weather));
        }

        [HttpGet("suggestionV2")]
        public Task<IActionResult> OutfitSuggestionV2(long userId, long? occasionId, string? weather = null)
        {
            return ValidateAndExecute(async () => await _outfitService.OutfitSuggestionV2(new OutfitSuggestionRequestModel()
            {
                OccasionId = occasionId,
                UserId = userId,
                Weather = weather
            }));
        }

        /// <summary>
        /// Virtual try-on feature - Apply clothing items to a human image using AI
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Form Data:**
        /// - `human`: Image file of the person (required, supported formats: jpg, jpeg, png)
        /// - `itemURLs`: List of clothing item image URLs to apply (required, at least 1 item)
        ///
        /// **Note:** This feature uses AI to generate a virtual try-on image showing how the clothing items would look on the person
        /// </remarks>
        [HttpPost("virtual-try-on")]
        [Consumes("multipart/form-data")]
        public Task<IActionResult> VirtualTryOn([FromForm] VirtualTryOnRequest model)
        {
            return ValidateAndExecute(async () => await _outfitService.VirtualTryOn(model.Human, model.ItemURLs));
        }
    }
}
