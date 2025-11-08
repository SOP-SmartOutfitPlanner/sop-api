using System;

namespace SOPServer.Service.BusinessModels.PostModels
{
    public class TopContributorModel
    {
        public long UserId { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public int PostCount { get; set; }
    }
}
