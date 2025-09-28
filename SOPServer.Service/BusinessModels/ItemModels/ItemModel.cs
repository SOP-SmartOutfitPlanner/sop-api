using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class ItemModel
    {
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

        public string Tag { get; set; }
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

        public string Tag { get; set; }
    }

    public class ItemModelAI
    {
        public string Color { get; set; }
        public string AiDescription { get; set; }
        public string WeatherSuitable { get; set; }
        public string Condition { get; set; }
        public string Pattern { get; set; }
        public string Fabric { get; set; }
    }

    public class ItemSummaryModel
    {
        public string Color { get; set; }
        public string AiDescription { get; set; }
        public string WeatherSuitable { get; set; }
        public string Condition { get; set; }
        public string Pattern { get; set; }
        public string Fabric { get; set; }
        public string ImageRemBgURL { get; set; }
    }
}
