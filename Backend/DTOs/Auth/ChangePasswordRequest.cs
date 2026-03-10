using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Old password is required.")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải từ 8-72 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
        [MaxLength(72, ErrorMessage = "Mật khẩu không được quá 72 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$", ErrorMessage = "Mật khẩu phải từ 8-72 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
