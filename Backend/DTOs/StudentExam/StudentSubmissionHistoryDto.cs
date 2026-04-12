namespace Backend.DTOs.StudentExam
{
    /// <summary>
    /// DTO thống nhất cho lịch sử bài nộp — gộp cả Kiểm tra và Luyện tập.
    /// </summary>
    public class StudentSubmissionHistoryDto
    {
        public int SubmissionId { get; set; }

        /// <summary>"Kiểm tra" hoặc "Luyện tập"</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Tên đề thi (kiểm tra) hoặc tên chương (luyện tập)</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Tên lớp học (nếu có)</summary>
        public string? ClassName { get; set; }

        /// <summary>Tên môn học</summary>
        public string SubjectName { get; set; } = string.Empty;

        public int TotalQuestions { get; set; }
        public int? CorrectCount { get; set; }
        public double? AccuracyRate { get; set; }
        public decimal? TotalPoints { get; set; }

        /// <summary>"Đã nộp" hoặc "Đang làm"</summary>
        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        /// <summary>ExamId (nếu là kiểm tra, để redirect xem kết quả)</summary>
        public int? ExamId { get; set; }
    }
}
