using SOPServer.Repository.Enums;

namespace SOPServer.Service.BusinessModels.ReportCommunityModels
{
    public class ReportFilterModel
    {
        public ReportType? Type { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
