using Backend.Common;
using Backend.Common.Models;
using Backend.DTOs.PracticeExam;

namespace Backend.Services.Interfaces
{
    public interface IPracticeExamService
    {
        /// <summary>
        /// Lấy danh sách chương + proficiency + số câu khả dụng cho Lớp học.
        /// </summary>
        Task<Result<List<ChapterProficiencyDto>>> GetChaptersForPracticeAsync(int classId);

        /// <summary>
        /// Tạo đề luyện tập tự động dựa trên proficiency.
        /// </summary>
        Task<Result<CreatePracticeExamResponse>> CreatePracticeExamAsync(CreatePracticeExamRequest request);

        /// <summary>
        /// Nộp bài luyện tập (không check thời gian).
        /// </summary>
        Task<Result<SubmitPracticeExamResponse>> SubmitPracticeExamAsync(SubmitPracticeExamRequest request);

        /// <summary>
        /// Lưu câu trả lời giữa chừng (không nộp bài) — giữ trạng thái InProgress.
        /// </summary>
        Task<Result> SavePracticeAnswersAsync(SubmitPracticeExamRequest request);

        /// <summary>
        /// Resume bài luyện tập đang làm dở — trả lại câu hỏi + câu trả lời đã lưu.
        /// </summary>
        Task<Result<ResumePracticeExamResponse>> ResumePracticeExamAsync(int submissionId);

        /// <summary>
        /// Xem kết quả + đáp án từng câu.
        /// </summary>
        Task<Result<PracticeExamResultDto>> GetPracticeResultAsync(int submissionId);

        /// <summary>
        /// Lấy lịch sử luyện tập.
        /// </summary>
        Task<Result<List<PracticeHistoryDto>>> GetPracticeHistoryAsync(int? classId);

        /// <summary>
        /// Phân tích quá trình luyện tập của toàn lớp — dành cho Giáo viên.
        /// </summary>
        Task<Result<ClassPracticeAnalyticsDto>> GetClassPracticeAnalyticsAsync(int classId);

        /// <summary>
        /// Phân tích quá trình luyện tập cá nhân của học sinh — dành cho Học sinh.
        /// </summary>
        Task<Result<StudentPracticeAnalyticsDto>> GetStudentPracticeAnalyticsAsync(int classId);
    }
}
