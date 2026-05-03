using System;

namespace Backend.DTOs.Class
{
    public class ExamInClassDTO
    {
        public int ExamId { get; set; }

        public string Title { get; set; } = null!;

        // Optional code/short id for the exam
        public string? Code { get; set; }

        // Subject display name
        public string SubjectName { get; set; } = string.Empty;

        // Teacher display name (optional)
        public string? TeacherName { get; set; }

        // Chapter id (nullable) - new: maps to a chapter if exam links to one via blueprint
        public int? ChapterId { get; set; }

        // When exam becomes visible/open/close
        public DateTime? VisibleFrom { get; set; }
        public DateTime? OpenAt { get; set; }
        public DateTime? CloseAt { get; set; }

        // Duration in minutes
        public int DurationMinutes { get; set; }

        // Exam status (Active/Upcoming/Closed numeric or string)
        public int Status { get; set; }

        // Flags for UI behaviour
        public int ShowScore { get; set; }
        public int ShowAnswer { get; set; }
        public int AnswerTimingMode { get; set; }
    }
}
