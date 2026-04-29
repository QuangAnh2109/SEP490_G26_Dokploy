using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Course
{
    public class AcceptInviteRequestDTO
    {
        [Required(ErrorMessage = "Token không được để trống")]
        public string? Token { get; set; }
    }
}
