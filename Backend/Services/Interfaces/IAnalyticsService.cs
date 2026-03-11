using Backend.DTOs.Analytics;
using System.Threading.Tasks;

namespace Backend.Services.Interfaces;

public interface IAnalyticsService
{
    /// <summary>
    /// Phân tích chi tiết bài thi cho Giáo viên:
    /// Điểm TB, phân bố, theo chương + độ khó, câu khó nhất, danh sách HS, đề xuất cải thiện.
    /// </summary>
    Task<ExamAnalyticsDetailDto> GetExamAnalyticsDetailAsync(int examId);

    /// <summary>
    /// Phân tích bài làm cá nhân cho Học sinh:
    /// Xem lại bài (luôn có), điểm + biểu đồ (ShowScore), đáp án (ShowAnswer).
    /// </summary>
    Task<StudentSubmissionAnalyticsDto> GetStudentSubmissionAnalyticsAsync(int examId, int studentId);
}
