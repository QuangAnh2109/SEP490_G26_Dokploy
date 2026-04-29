using Backend.Common.Models;
using Backend.DTOs.Analytics;
using System.Threading.Tasks;

namespace Backend.Services.Interfaces;

public interface IAnalyticsService
{
    /// <summary>
    /// Phân tích chi tiết bài thi cho Giáo viên:
    /// Điểm TB, phân bố, theo chương + độ khó, câu khó nhất, danh sách HS, đề xuất cải thiện.
    /// </summary>
    Task<Result<ExamAnalyticsDetailDto>> GetExamAnalyticsDetailAsync(int examId);

    /// <summary>
    /// Phân tích bài làm cá nhân cho Học sinh:
    /// Xem lại bài (luôn có), điểm + biểu đồ (ShowScore), đáp án (ShowAnswer).
    /// </summary>
    Task<Result<StudentSubmissionAnalyticsDto>> GetStudentSubmissionAnalyticsAsync(int examId, int studentId);

    /// <summary>
    /// Thống kê nộp bài cho Giáo viên: danh sách học sinh, lần nộp, điểm, trạng thái.
    /// </summary>
    Task<Result<ExamSubmitResultsDto>> GetExamSubmitResultsAsync(int examId);

    /// <summary>
    /// Xem chi tiết bài làm theo submissionId — dành cho Giáo viên (luôn ShowScore + ShowAnswer).
    /// </summary>
    Task<Result<StudentSubmissionAnalyticsDto>> GetSubmissionBySubmissionIdAsync(int submissionId);
}
