using SOPServer.Repository.Enums;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ReportCommunityModels
{
    public class ResolveWithActionModel
    {
        [Required(ErrorMessage = "Action is required")]
        public ReportAction Action { get; set; }

        [Required(ErrorMessage = "Notes are required")]
        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string Notes { get; set; } = string.Empty;

        [Range(1, 365, ErrorMessage = "Suspension days must be between 1 and 365")]
        public int? SuspensionDays { get; set; }
    }
}
