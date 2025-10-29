using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.StyleModels
{
    public class StyleUpdateModel
    {
        [Required]
        public long Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }
    }
}
