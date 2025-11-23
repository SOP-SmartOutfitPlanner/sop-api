namespace SOPServer.Service.BusinessModels.QDrantModels
{
    /// <summary>
    /// Compact item model optimized for AI processing and outfit selection
    /// </summary>
    public class ItemForAISelection
    {
        public long Id { get; set; }
        public float Score { get; set; }
        
        /// <summary>
        /// Compact string representation of item details for AI consumption
        /// Format: "ID:{id}|Cat:{category}|Color:{color}|Desc:{description}|Weather:{weather}|Cond:{condition}|Pattern:{pattern}|Fabric:{fabric}|Occasions:{occasions}|Seasons:{seasons}|Styles:{styles}"
        /// </summary>
        public string ItemSummary { get; set; }

        /// <summary>
        /// Build compact item summary from item details
        /// </summary>
        public static string BuildItemSummary(
            long id,
            string categoryName,
            string aiDescription,
            string weatherSuitable,
            List<string> occasions,
            List<string> seasons,
            List<string> styles)
        {
            var parts = new List<string>
            {
                $"ID:{id}",
                !string.IsNullOrEmpty(categoryName) ? $"Cat:{categoryName}" : null,
                !string.IsNullOrEmpty(aiDescription) ? $"Desc:{aiDescription}" : null,
                !string.IsNullOrEmpty(weatherSuitable) ? $"Weather:{weatherSuitable}" : null,
                occasions?.Any() == true ? $"Occasions:{string.Join(",", occasions)}" : null,
                seasons?.Any() == true ? $"Seasons:{string.Join(",", seasons)}" : null,
                styles?.Any() == true ? $"Styles:{string.Join(",", styles)}" : null
            };

            return string.Join("|", parts.Where(p => p != null));
        }
    }
}
