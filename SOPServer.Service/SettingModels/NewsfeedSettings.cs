using System;

namespace SOPServer.Service.SettingModels
{
    public class NewsfeedSettings
    {
        public double RecencyWeight { get; set; }
        public double EngagementWeight { get; set; }
        public int RecencyWindowHour { get; set; }
        public int CommentMultiplier { get; set; }
        public int LookbackDays { get; set; }
    }
}
