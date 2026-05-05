using System;

namespace Backend.DTOs.Class
{
    public class StudentInClassDTO
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? StudentCode { get; set; }
        public DateTime JoinedAtUtc { get; set; }
    }
}
