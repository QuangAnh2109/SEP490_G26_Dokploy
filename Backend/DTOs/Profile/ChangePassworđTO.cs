using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Profile;

public class ChangePasswordDTO
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(72, MinimumLength = 8,
        ErrorMessage = "Mật khẩu phải từ 8 đến 72 ký tự")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,72}$",
        ErrorMessage = "Mật khẩu phải có chữ hoa, chữ thường, số và ký tự đặc biệt"
    )]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
    [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp")]
    public string ConfirmPassword { get; set; } = null!;
}