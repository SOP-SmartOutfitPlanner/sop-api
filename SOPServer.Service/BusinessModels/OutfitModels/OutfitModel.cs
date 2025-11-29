using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.SeasonModels;
using SOPServer.Service.BusinessModels.StyleModels;
using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.OutfitModels
{
    public class OutfitModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsSaved { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public List<OutfitItemModel> Items { get; set; } = new List<OutfitItemModel>();
    }

    public class OutfitDetailedModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsSaved { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public List<OutfitItemModel> Items { get; set; } = new List<OutfitItemModel>();
    }

    public class OutfitItemModel
    {
        public long ItemId { get; set; }
        public string Name { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Color { get; set; }
        public string AiDescription { get; set; }
        public string Brand { get; set; }
        public string FrequencyWorn { get; set; }
        public DateTime? LastWornAt { get; set; }
        public string ImgUrl { get; set; }
        public string WeatherSuitable { get; set; }
        public string Condition { get; set; }
        public string Pattern { get; set; }
        public string Fabric { get; set; }
        public ItemType ItemType { get; set; }
        public List<OccasionItemModel> Occasions { get; set; } = new List<OccasionItemModel>();
        public List<SeasonItemModel> Seasons { get; set; } = new List<SeasonItemModel>();
        public List<StyleItemModel> Styles { get; set; } = new List<StyleItemModel>();
    }
}
