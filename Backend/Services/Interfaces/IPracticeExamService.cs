using Backend.DTOs.PracticeExam;

namespace Backend.Services.Interfaces
{
    public interface IPracticeExamService
    {
        /// <summary>
        /// Lấy danh sách chương + proficiency + số câu khả dụng cho khóa học.
        /// </summary>
        Task<List<ChapterProficiencyDto>> GetChaptersForPracticeAsync(int classId, int studentId);

        /// <summary>
        /// Tạo đề luyện tập tự động dựa trên proficiency.
        /// </summary>
        Task<CreatePracticeExamResponse> CreatePracticeExamAsync(int studentId, CreatePracticeExamRequest request);

        /// <summary>
        /// Nộp bài luyện tập (không check thời gian).
        /// </summary>
        Task<SubmitPracticeExamResponse> SubmitPracticeExamAsync(int studentId, SubmitPracticeExamRequest request);

        /// <summary>
        /// Lưu câu trả lời giữa chừng (không nộp bài) — giữ trạng thái InProgress.
        /// </summary>
        Task SavePracticeAnswersAsync(int studentId, SubmitPracticeExamRequest request);

        /// <summary>
        /// Resume bài luyện tập đang làm dở — trả lại câu hỏi + câu trả lời đã lưu.
        /// </summary>
        Task<ResumePracticeExamResponse> ResumePracticeExamAsync(int submissionId, int studentId);

        /// <summary>
        /// Xem kết quả + đáp án từng câu.
        /// </summary>
        Task<PracticeExamResultDto> GetPracticeResultAsync(int submissionId, int studentId);

        /// <summary>
        /// Lấy lịch sử luyện tập.
        /// </summary>
        Task<List<PracticeHistoryDto>> GetPracticeHistoryAsync(int studentId, int? classId);
    }
}
