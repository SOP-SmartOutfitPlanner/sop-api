using System;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.SaveItemFromPostModels
{
    public class SaveItemFromPostCreateModel
    {
        [Required(ErrorMessage = "ItemId is required")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "PostId is required")]
        public long PostId { get; set; }
    }
}
