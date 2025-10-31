using SOPServer.Service.BusinessModels.CategoryModels;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.SeasonModels;
using SOPServer.Service.BusinessModels.StyleModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class ItemModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string Name { get; set; }

        public long CategoryId { get; set; }

        public string CategoryName { get; set; }

        public string Color { get; set; }

        public string AiDescription { get; set; }

        public string Brand { get; set; }

        public string FrequencyWorn { get; set; }

        public DateTime LastWornAt { get; set; }

        public string ImgUrl { get; set; }

        public string WeatherSuitable { get; set; }

        public string Condition { get; set; }

        public string Pattern { get; set; }

        public string Fabric { get; set; }

        public List<OccasionItemModel> Occasions { get; set; }

        public List<SeasonItemModel> Seasons { get; set; }

        public List<StyleItemModel> Styles { get; set; }
    }

    public class ItemCreateModel
    {
        public long UserId { get; set; }
        public string Name { get; set; }

        public long CategoryId { get; set; }

        public string CategoryName { get; set; }

        public string Color { get; set; }

        public string AiDescription { get; set; }

        public string Brand { get; set; }

        public string FrequencyWorn { get; set; }

        public DateTime LastWornAt { get; set; }

        public string ImgUrl { get; set; }

        public string WeatherSuitable { get; set; }

        public string Condition { get; set; }

        public string Pattern { get; set; }

        public string Fabric { get; set; }

        // Optional relationship IDs
        public List<long> StyleIds { get; set; } = new List<long>();
        public List<long> OccasionIds { get; set; } = new List<long>();
        public List<long> SeasonIds { get; set; } = new List<long>();
    }

    public class ItemModelAI
    {
        public List<ColorModel> Colors { get; set; }
        public string AiDescription { get; set; }
        public string WeatherSuitable { get; set; }
        public string Condition { get; set; }
        public string Pattern { get; set; }
        public string Fabric { get; set; }
        public CategoryItemModel Category { get; set; }
        public List<StyleItemModel> Styles { get; set; }
        public List<OccasionItemModel> Occasions { get; set; }
        public List<SeasonItemModel> Seasons { get; set; }

    }

    public class ItemSummaryModel
    {
        public List<ColorModel> Colors { get; set; }
        public string AiDescription { get; set; }
        public string WeatherSuitable { get; set; }
        public string Condition { get; set; }
        public string Pattern { get; set; }
        public string Fabric { get; set; }
        public string ImageRemBgURL { get; set; }
        public CategoryItemModel Category { get; set; }
        public List<StyleItemModel> Styles { get; set; }
        public List<OccasionItemModel> Occasions { get; set; }
        public List<SeasonItemModel> Seasons { get; set; }
    }
    public class ColorModel
    {
        public string Name { get; set; }
        public string Hex { get; set; }
    }
}
