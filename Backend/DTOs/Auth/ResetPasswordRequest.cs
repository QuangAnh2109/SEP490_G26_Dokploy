using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP code is required.")]
        public string OtpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
