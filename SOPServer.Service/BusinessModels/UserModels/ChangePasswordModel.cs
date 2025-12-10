using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.UserModels
{
    /// <summary>
    /// Request model for changing password with current password verification
    /// </summary>
    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "New password and confirm password do not match")]
        public string ConfirmPassword { get; set; }
    }

    /// <summary>
    /// Request model for changing password with OTP verification
    /// </summary>
    public class ChangePasswordWithOtpModel
    {
        [Required(ErrorMessage = "OTP is required")]
        public string Otp { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "New password and confirm password do not match")]
        public string ConfirmPassword { get; set; }
    }

    /// <summary>
    /// Request model to initiate OTP-based password change
    /// </summary>
    public class InitiateChangePasswordOtpModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
    }
}
