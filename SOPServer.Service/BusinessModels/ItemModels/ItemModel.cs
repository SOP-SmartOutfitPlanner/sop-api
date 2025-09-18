using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class ItemModel
    {
        public string Name { get; set; }

        public string Image { get; set; }

        public long? UserId { get; set; }

        public string Color { get; set; }

        public string AiDescription { get; set; }

        public string Brand { get; set; }

        public string FrequencyWorn { get; set; }

        public string LastWornAt { get; set; }

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
}
