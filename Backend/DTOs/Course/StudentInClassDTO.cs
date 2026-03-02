using System;

namespace Backend.DTOs.Course
{
    public class StudentInClassDTO
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime JoinedAtUtc { get; set; }
    }
}
