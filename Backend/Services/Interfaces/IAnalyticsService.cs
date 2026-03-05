using Backend.DTOs.Analytics;
using System.Threading.Tasks;

namespace Backend.Services.Interfaces;

public interface IAnalyticsService
{
    /// <summary>
    /// Tính toán và đưa ra phân tích chi tiết kết quả môn Toán theo từng chương cho 1 bài thi.
    /// Kèm theo Đề xuất tự động (Actionable Insights).
    /// </summary>
    /// <param name="examId">ID của bài thi</param>
    /// <returns>ExamAnalyticsDto chứa số liệu và Lời khuyên</returns>
    Task<ExamAnalyticsDto> GetExamAnalyticsAsync(int examId);
}
