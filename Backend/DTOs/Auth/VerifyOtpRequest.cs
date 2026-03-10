using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs
{
    public class VerifyOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải là 6 chữ số.")]
        public string OtpCode { get; set; } = string.Empty;
    }
}
