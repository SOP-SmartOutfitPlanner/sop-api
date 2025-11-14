using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.BusinessModels.UserOccasionModels;
using System;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.OutfitCalendarModels
{
    /// <summary>
    /// Represents multiple outfits grouped by a single user occasion
    /// </summary>
    public class OutfitCalendarGroupedModel
    {
        /// <summary>
        /// The user occasion details (including date, time, etc.)
        /// </summary>
        public UserOccasionModel UserOccasion { get; set; }

        /// <summary>
        /// Indicates if this is a daily outfit schedule
        /// </summary>
        public bool IsDaily { get; set; }

        /// <summary>
        /// List of outfits scheduled for this occasion
        /// </summary>
        public List<OutfitCalendarItemModel> Outfits { get; set; } = new List<OutfitCalendarItemModel>();
    }

    /// <summary>
    /// Individual outfit within a calendar group
    /// </summary>
    public class OutfitCalendarItemModel
    {
        public long CalendarId { get; set; }
        public long OutfitId { get; set; }
        public string OutfitName { get; set; }
        public OutfitModel OutfitDetails { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
