using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Course
{
    public class InviteStudentRequestDTO
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;
    }
}
