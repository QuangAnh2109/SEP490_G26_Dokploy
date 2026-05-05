using Backend.DTOs.PracticeExam;
using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IPracticeExamRepository
    {
        /// <summary>
        /// Lấy class + subject + ClassMembers (kèm Student) để phân tích luyện tập lớp học.
        /// teacherId=0 để bỏ qua kiểm tra ownership (dùng cho Student lookup).
        /// </summary>
        Task<(Class? Cls, List<ClassMember> Members)> GetClassWithMembersAsync(int classId, int teacherId);

        /// <summary>
        /// Lấy toàn bộ phiên luyện tập đã nộp của danh sách học sinh, lọc theo môn học.
        /// Trả về per-session với danh sách câu hỏi đúng/sai đã được tính.
        /// </summary>
        Task<List<PracticeSessionRaw>> GetPracticeSessionsAsync(List<int> studentIds, int subjectId);


        /// <summary>
        /// Lấy thông tin Class (SubjectId, TeacherId) + validate sinh viên thuộc lớp.
        /// </summary>
        Task<Class?> GetClassWithValidationAsync(int classId, int studentId);

        /// <summary>
        /// Lấy proficiency data từ submissions đã nộp, group by (ChapterId, Difficulty).
        /// </summary>
        Task<List<StudentProficiencyRaw>> GetStudentProficiencyAsync(int studentId, List<int> chapterIds);

        /// <summary>
        /// Lấy toàn bộ Question IDs luyện tập khả dụng cho các chương đã chọn (Purpose=2, Status=Active, CreatedBy=teacherId).
        /// Có thể lọc theo mức độ câu hỏi.
        /// </summary>
        Task<List<int>> GetAllPracticeQuestionIdsAsync(List<int> chapterIds, int teacherId, List<int>? difficultyLevels = null);

        /// <summary>
        /// Lấy lịch sử làm đúng/sai từng câu hỏi của sinh viên trong các chương cụ thể (phục vụ Spaced Repetition).
        /// </summary>
        Task<List<PracticeQuestionResultDto>> GetStudentItemLevelHistoryAsync(int studentId, List<int> chapterIds);

        /// <summary>
        /// Đếm số câu hỏi luyện tập khả dụng theo chương (Purpose=2, Status=Active, CreatedBy=teacherId).
        /// Có thể lọc theo mức độ câu hỏi.
        /// </summary>
        Task<int> CountPracticeQuestionsAsync(int chapterId, int teacherId, List<int>? difficultyLevels = null);

        /// <summary>
        /// Đếm số câu hỏi luyện tập theo từng (ChapterId, Difficulty) trong 1 query (tránh N+1).
        /// </summary>
        Task<List<PracticeQuestionCountRaw>> GetPracticeQuestionCountsAsync(List<int> chapterIds, int teacherId);

        /// <summary>
        /// Tạo Paper (ExamId=null) + gắn câu hỏi.
        /// </summary>
        Task<Paper> CreatePracticePaperAsync(List<int> questionIds);

        /// <summary>
        /// Tạo Submission cho paper luyện tập.
        /// </summary>
        Task<Submission> CreatePracticeSubmissionAsync(int studentId, int paperId);

        /// <summary>
        /// Lấy Submission kèm Paper, Questions, QuestionAnswers cho submit/result.
        /// Đặt <paramref name="tracked"/>=true khi cần ghi (Submit/Save) — mặc định AsNoTracking cho read paths.
        /// </summary>
        Task<Submission?> GetPracticeSubmissionFullAsync(int submissionId, int studentId, bool tracked = false);

        /// <summary>
        /// Lấy lịch sử luyện tập (Paper.ExamId == null).
        /// </summary>
        Task<List<PracticeHistoryRaw>> GetPracticeHistoryAsync(int studentId, int? classId);

        /// <summary>
        /// Lấy Paper luyện tập kèm Questions + QuestionAnswers + BlankInputs.
        /// </summary>
        Task<Paper?> GetPracticePaperWithQuestionsAsync(int paperId);

        /// <summary>
        /// Lấy chapters theo subjectId.
        /// </summary>
        Task<List<Chapter>> GetChaptersBySubjectIdAsync(int subjectId);

        /// <summary>
        /// Lưu thay đổi vào DB.
        /// </summary>
        Task SaveChangesAsync();
    }

    // ─── Raw data classes ───────────────────────────────────────────────

    public class StudentProficiencyRaw
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public int TotalAttempted { get; set; }
        public int CorrectCount { get; set; }
    }

    public class PracticeQuestionCountRaw
    {
        public int ChapterId { get; set; }
        public int Difficulty { get; set; }
        public int Count { get; set; }
    }

    public class PracticeHistoryRaw
    {
        public int SubmissionId { get; set; }
        public int PaperId { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string? SubjectCode { get; set; }
        public List<string> ChapterNames { get; set; } = new();
        public int TotalQuestions { get; set; }
        public int? CorrectCount { get; set; }
        public decimal? TotalPoints { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public int Status { get; set; }
    }
}
