using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ReportCommunityModels
{
    public class ResolveNoViolationModel
    {
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }
    }
}
