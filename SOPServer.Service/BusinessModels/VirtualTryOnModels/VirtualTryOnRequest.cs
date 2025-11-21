using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.VirtualTryOnModels
{
    /// <summary>
    /// Request model for virtual try-on feature
    /// </summary>
    public class VirtualTryOnRequest
    {
        /// <summary>
        /// Image file of the person
        /// </summary>
        /// <example>person.jpg</example>
        [Required(ErrorMessage = "Human image is required")]
        public IFormFile Human { get; set; }

        /// <summary>
        /// List of clothing item image URLs to apply
        /// </summary>
        /// <example>["https://example.com/item1.jpg", "https://example.com/item2.jpg"]</example>
        [Required(ErrorMessage = "At least one item URL is required")]
        [MinLength(1, ErrorMessage = "At least one item URL is required")]
        public List<string> ItemURLs { get; set; } = new List<string>();
    }
}
