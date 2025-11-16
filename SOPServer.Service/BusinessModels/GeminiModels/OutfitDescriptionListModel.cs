using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.GeminiModels
{
  /// <summary>
    /// Model for AI-generated outfit item descriptions
    /// Used to receive structured outfit suggestions from Gemini AI
    /// </summary>
    public class OutfitDescriptionListModel
    {
        /// <summary>
     /// List of detailed descriptions for each item in the suggested outfit
      /// Each description should match the format used in Item embedding (color, category, style, occasion, season, etc.)
        /// </summary>
        public List<string> ItemDescriptions { get; set; } = new List<string>();
    }
}
