using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã OTP là bắt buộc.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải là 6 chữ số.")]
        public string OtpCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải từ 8-72 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
        [MaxLength(72, ErrorMessage = "Mật khẩu không được quá 72 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$", ErrorMessage = "Mật khẩu phải từ 8-72 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
