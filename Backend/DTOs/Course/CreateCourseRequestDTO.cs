using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Course
{
    public class CreateCourseRequestDTO
    {
        [Required(ErrorMessage = "Class Name is required")]
        [StringLength(50, ErrorMessage = "Class Name cannot exceed 50 characters")]
        public string ClassName { get; set; } = null!;

        [Required(ErrorMessage = "Subject is required")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Semester is required")]
        [StringLength(20, ErrorMessage = "Semester cannot exceed 20 characters")]
        public string Semester { get; set; } = null!;
    }
}
