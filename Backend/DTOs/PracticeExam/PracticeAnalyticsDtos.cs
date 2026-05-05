namespace Backend.DTOs.PracticeExam
{
    // ═══════════════════════════════════════════════════════════
    //  TEACHER — Phân tích luyện tập theo lớp
    // ═══════════════════════════════════════════════════════════

    public class ClassPracticeAnalyticsDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int TotalSessions { get; set; }
        public double AverageAccuracyRate { get; set; }
        public List<StudentPracticeStatDto> StudentStats { get; set; } = new();
        public List<PracticeChapterStatDto> ChapterStats { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class StudentPracticeStatDto
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public int TotalQuestionsAttempted { get; set; }
        public int CorrectCount { get; set; }
        public double AccuracyRate { get; set; }
        public DateTime? LastPracticeAt { get; set; }
        public string ProficiencyLevel { get; set; } = string.Empty;
        public List<StudentChapterPracticeStatDto> ChapterBreakdown { get; set; } = new();
    }

    public class StudentChapterPracticeStatDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public int TotalAttempted { get; set; }
        public int CorrectCount { get; set; }
        public double AccuracyRate => TotalAttempted > 0
            ? Math.Round((double)CorrectCount / TotalAttempted * 100, 1) : 0;
        public string ProficiencyLevel => AccuracyRate < 50 ? "Yếu"
            : AccuracyRate < 80 ? "Trung bình" : "Mạnh";
    }

    public class PracticeChapterStatDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public int StudentPracticed { get; set; }
        public int TotalAttempts { get; set; }
        public int CorrectCount { get; set; }
        public double AccuracyRate => TotalAttempts > 0
            ? Math.Round((double)CorrectCount / TotalAttempts * 100, 1) : 0;
        public string Status => AccuracyRate < 50 ? "Báo động"
            : AccuracyRate < 70 ? "Cần chú ý" : "Tốt";
    }

    // ═══════════════════════════════════════════════════════════
    //  STUDENT — Phân tích luyện tập cá nhân
    // ═══════════════════════════════════════════════════════════

    public class StudentPracticeAnalyticsDto
    {
        public int ClassId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public int TotalQuestionsAttempted { get; set; }
        public int CorrectCount { get; set; }
        public double OverallAccuracyRate { get; set; }
        public DateTime? LastPracticeAt { get; set; }
        public List<StudentChapterPracticeStatDto> ChapterStats { get; set; } = new();
        public List<DifficultyPracticeStatDto> DifficultyStats { get; set; } = new();
        public List<PracticeTrendDto> TrendData { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class DifficultyPracticeStatDto
    {
        public int Difficulty { get; set; }
        public string DifficultyName { get; set; } = string.Empty;
        public int TotalAttempted { get; set; }
        public int CorrectCount { get; set; }
        public double AccuracyRate => TotalAttempted > 0
            ? Math.Round((double)CorrectCount / TotalAttempted * 100, 1) : 0;
    }

    public class PracticeTrendDto
    {
        public string DateLabel { get; set; } = string.Empty;
        public double AccuracyRate { get; set; }
        public int QuestionsAttempted { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    //  RAW — Dữ liệu thô từ Repository
    // ═══════════════════════════════════════════════════════════

    public class PracticeSessionRaw
    {
        public int SubmissionId { get; set; }
        public int StudentId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public List<PracticeQuestionAnswerRaw> QuestionAnswers { get; set; } = new();
    }

    public class PracticeQuestionAnswerRaw
    {
        public int QuestionId { get; set; }
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public bool IsCorrect { get; set; }
    }
}
