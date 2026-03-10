using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth
{
    public class ResendOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
