using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.StyleModels
{
    public class StyleCreateModel
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }
    }
}
