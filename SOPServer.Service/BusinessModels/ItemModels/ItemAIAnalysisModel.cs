using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.SeasonModels;
using SOPServer.Service.BusinessModels.StyleModels;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    /// <summary>
    /// Complete AI Analysis result stored in AIAnalyzeJson field
    /// </summary>
    public class ItemAIAnalysisModel
    {
        // Category information (from BulkCreateItemAuto)
        public long CategoryId { get; set; }

        // Item properties (from AnalysisItem)
        public List<ColorModel> Colors { get; set; }
        public string AiDescription { get; set; }
        public string WeatherSuitable { get; set; }
        public string Condition { get; set; }
        public string Pattern { get; set; }
        public string Fabric { get; set; }
        public List<StyleItemModel> Styles { get; set; }
        public List<OccasionItemModel> Occasions { get; set; }
        public List<SeasonItemModel> Seasons { get; set; }
        public int Confidence { get; set; }
    }
}
