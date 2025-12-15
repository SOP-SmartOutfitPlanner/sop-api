using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.UserModels
{
    /// <summary>
    /// Request model for validating full body image for virtual try-on
    /// </summary>
    public class ValidateFullBodyImageRequest
    {
        /// <summary>
        /// URL of the image to validate
        /// </summary>
        /// <example>https://example.com/images/full-body.jpg</example>
        [Required(ErrorMessage = "Image URL is required")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string ImageUrl { get; set; }
    }
}
