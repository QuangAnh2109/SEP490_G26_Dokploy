using Backend.DTOs.Analytics;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Services.Implements;

public class AnalyticsService : IAnalyticsService
{
    private readonly MtcaSep490G26Context _context;

    public AnalyticsService(MtcaSep490G26Context context)
    {
        _context = context;
    }

    public async Task<ExamAnalyticsDto> GetExamAnalyticsAsync(int examId)
    {
        var exam = await _context.Exams
            .Include(e => e.Papers)
                .ThenInclude(p => p.Submissions)
                    .ThenInclude(s => s.StudentAnswers)
                        .ThenInclude(sa => sa.QuestionAnswer)
                            .ThenInclude(qa => qa.Question)
                                .ThenInclude(q => q.Chapter)
            .Include(e => e.Papers)
                .ThenInclude(p => p.Questions)
                    .ThenInclude(q => q.Chapter)
            .Include(e => e.Papers)
                .ThenInclude(p => p.Questions)
                    .ThenInclude(q => q.QuestionAnswers)
            .FirstOrDefaultAsync(e => e.ExamId == examId);

        if (exam == null)
            throw new KeyNotFoundException($"Không tìm thấy bài thi với ID {examId}.");

        var allSubmissions = exam.Papers.SelectMany(p => p.Submissions)
            // Lọc ra các bài đã nộp (Status == 1 - Submitted là ví dụ, tùy logic hiện tại của bạn)
            .ToList();

        var dto = new ExamAnalyticsDto
        {
            ExamId = exam.ExamId,
            ExamTitle = exam.Title,
            TotalSubmissions = allSubmissions.Count
        };

        if (dto.TotalSubmissions == 0)
        {
            dto.Recommendations.Add("Chưa có học sinh nào nộp bài thi này để phân tích.");
            return dto;
        }

        // Tạo Dictionary để tra cứu nhanh câu hỏi -> Chapter -> Đáp án đúng
        // Ở đây giả sử Câu hỏi Trắc Nghiệm thì ResponseText == Answer
        // Còn Tự luận thì có thể phức tạp hơn, nhưng hiện tại ta dựa trên logic text mapping đơn giản
        // Hoặc dựa trên điểm trong StudentAnswer (nếu có lưu Point). Trong schema StudentAnswer không có Point,
        // Vậy nên ta tạm thời so sánh (ResponseText == Question.Answer) để xem là làm Đúng.

        var questionDict = exam.Papers
            .SelectMany(p => p.Questions)
            .Distinct()
            .ToDictionary(q => q.QuestionId, q => new { q.Chapter, q.QuestionAnswers, q.QuestionType });

        // Nhóm tất cả các câu trả lời học sinh theo Chapter
        // Cần truy vết từ StudentAnswer -> Submission -> Paper -> PaperQuestion -> Question -> Chapter
        // GroupBy Chapter
        var chapterGroupings = allSubmissions
            .SelectMany(s => s.StudentAnswers)
            .Select(ans =>
            {
                // Use QuestionAnswer -> Question -> Chapter to trace back
                var questionAnswer = ans.QuestionAnswer;
                if (questionAnswer == null) return null;

                var question = questionAnswer.Question;
                if (question == null) return null;

                if (!questionDict.TryGetValue(question.QuestionId, out var qInfo)) return null;

                // Check if the student's response matches the correct answer
                bool isCorrect = false;
                if (!string.IsNullOrEmpty(questionAnswer.CorrectAnswer) && !string.IsNullOrEmpty(ans.Response))
                {
                     isCorrect = questionAnswer.CorrectAnswer.Trim().Equals(ans.Response.Trim(), System.StringComparison.OrdinalIgnoreCase);
                }
                else if (questionAnswer.IsCorrect.HasValue)
                {
                    // For multiple choice, check IsCorrect flag
                    isCorrect = questionAnswer.IsCorrect.Value && !string.IsNullOrEmpty(ans.Response);
                }

                return new
                {
                    ChapterId = qInfo.Chapter.ChapterId,
                    ChapterName = qInfo.Chapter.Name,
                    IsCorrect = isCorrect
                };
            })
            .Where(x => x != null)
            .GroupBy(x => x!.ChapterId)
            .ToList();

        foreach (var group in chapterGroupings)
        {
            var firstItem = group.First()!; // Biết chắc chắn không null vì Where rồi
            var stat = new ChapterAnalyticsDto
            {
                ChapterId = group.Key,
                ChapterName = firstItem.ChapterName,
                TotalAnswers = group.Count(),
                CorrectAnswers = group.Count(x => x!.IsCorrect)
            };
            dto.ChapterStats.Add(stat);
        }

        // Sinh ra các Đề xuất Tự động (Actionable Insights) dựa vào Threshold
        foreach (var stat in dto.ChapterStats)
        {
            if (stat.Status == "Báo động")
            {
                dto.Recommendations.Add($"🚨 CẢNH BÁO: Lớp đang hổng kiến thức rất nặng ở Chương gốc [{stat.ChapterName}] (Tỉ lệ làm đúng chỉ đạt {stat.AccuracyRate}%). Đề xuất: Giáo viên cần tổ chức ôn tập lại lý thuyết và công thức cơ bản của chương này vào tiết học tới trước khi chuyển sang kiến thức mới.");
            }
            else if (stat.Status == "Cần chú ý")
            {
                dto.Recommendations.Add($"⚠️ LƯU Ý: Kỹ năng giải bài tập thuộc [{stat.ChapterName}] đang ở mức Trung bình ({stat.AccuracyRate}%). Đề xuất: Giáo viên nên giao thêm Bài tập về nhà mức độ cơ bản (Mini-test) tập trung riêng vào chương này để học sinh rèn luyện tính toán, tránh sai sót.");
            }
            else if (stat.Status == "Tốt")
            {
                dto.Recommendations.Add($"🌟 ĐIỂM SÁNG: Lớp nắm rất vững kiến thức [{stat.ChapterName}] (Tỉ lệ làm đúng đạt tới {stat.AccuracyRate}%). Đề xuất: Giáo viên có thể lướt nhanh phần lý thuyết chương này và bổ sung thêm các bài tập Vận dụng cao (Level 3, Level 4) để bồi dưỡng tư duy.");
            }
        }

        return dto;
    }
}
