using Backend.Constants;
using Backend.DTOs.Analytics;
using Backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Backend.Common;

/// <summary>
/// Helper methods cho module Analytics: kiểm tra đúng/sai, tính median, phân bố điểm, sinh đề xuất.
/// </summary>
public static class AnalyticsHelper
{
    public static bool CheckIsCorrect(QuestionAnswer qa, StudentAnswer sa)
    {
        if (!string.IsNullOrEmpty(qa.CorrectAnswer) && !string.IsNullOrEmpty(sa.Response))
            return qa.CorrectAnswer.Trim().Equals(sa.Response.Trim(), StringComparison.OrdinalIgnoreCase);

        if (qa.IsCorrect.HasValue)
            return qa.IsCorrect.Value && !string.IsNullOrEmpty(sa.Response);

        return false;
    }

    /// <summary>
    /// Chấm điểm bài làm (áp dụng chung cho cả Thi thật và Luyện tập).
    /// Yêu cầu: submission phải được eager load Paper.Questions.QuestionAnswers và StudentAnswers.
    /// Trả về điểm trên thang 10.
    /// </summary>
    public static (int CorrectCount, int TotalQuestions, decimal TotalPoints) GradeSubmission(Submission submission)
    {
        var paper = submission.Paper;
        if (paper == null || paper.Questions == null) return (0, 0, 0);

        var evaluatedQuestions = EvaluateSubmission(paper.Questions.DistinctBy(q => q.QuestionId), submission.StudentAnswers);
        
        int correctCount = evaluatedQuestions.Count(q => q.IsCorrect);
        int totalQuestions = evaluatedQuestions.Count;

        decimal totalPoints = totalQuestions > 0
            ? Math.Round((decimal)correctCount / totalQuestions * 10, 3)
            : 0;

        return (correctCount, totalQuestions, totalPoints);
    }

    public record EvaluatedQuestion(
        int QuestionId,
        string QuestionContent,
        string QuestionType,
        int ChapterId,
        string ChapterName,
        int Difficulty,
        bool IsCorrect,
        List<EvaluatedOption> Options
    );

    public record EvaluatedOption(
        int QuestionAnswerId,
        string Content,
        string? StudentResponse,
        bool IsSelected,
        bool? IsCorrect,
        string? CorrectAnswer
    );

    public static List<EvaluatedQuestion> EvaluateSubmission(IEnumerable<Question> questions, IEnumerable<StudentAnswer> studentAnswers)
    {
        var result = new List<EvaluatedQuestion>();
        
        foreach (var question in questions)
        {
            bool questionCorrect = true;
            var options = new List<EvaluatedOption>();

            foreach (var qa in question.QuestionAnswers)
            {
                var sa = studentAnswers.FirstOrDefault(a => a.QuestionAnswerId == qa.QuestionAnswerId);
                if (sa != null)
                {
                    if (!CheckIsCorrect(qa, sa))
                        questionCorrect = false;
                }
                else if (qa.IsCorrect == true)
                {
                    questionCorrect = false;
                }

                options.Add(new EvaluatedOption(
                    qa.QuestionAnswerId,
                    qa.Content,
                    sa?.Response,
                    sa != null,
                    qa.IsCorrect,
                    qa.CorrectAnswer
                ));
            }

            result.Add(new EvaluatedQuestion(
                question.QuestionId,
                question.QuestionContent,
                question.QuestionType,
                question.Chapter?.ChapterId ?? 0,
                question.Chapter?.Name ?? "N/A",
                question.Difficulty,
                questionCorrect,
                options
            ));
        }

        return result;
    }

    public static decimal GetMedian(List<decimal> sorted)
    {
        int count = sorted.Count;
        if (count == 0) return 0;
        if (count % 2 == 0)
            return Math.Round((sorted[count / 2 - 1] + sorted[count / 2]) / 2, 2);
        return sorted[count / 2];
    }

    public static Dictionary<string, int> BuildScoreDistribution(List<decimal> scores)
    {
        var dist = new Dictionary<string, int>
        {
            ["0-1"] = 0,  ["1-2"] = 0,  ["2-3"] = 0,  ["3-4"] = 0,  ["4-5"] = 0,
            ["5-6"] = 0,  ["6-7"] = 0,  ["7-8"] = 0,  ["8-9"] = 0,  ["9-10"] = 0
        };

        foreach (var score in scores)
        {
            var bucket = score switch
            {
                < 1 => "0-1",
                < 2 => "1-2",
                < 3 => "2-3",
                < 4 => "3-4",
                < 5 => "4-5",
                < 6 => "5-6",
                < 7 => "6-7",
                < 8 => "7-8",
                < 9 => "8-9",
                _ => "9-10"
            };
            dist[bucket]++;
        }
        return dist;
    }

    public record AnswerResult(
        int QuestionId,
        string QuestionContent,
        int ChapterId,
        string ChapterName,
        int Difficulty,
        bool IsCorrect);

    public static AnswerResult? MapStudentAnswer(StudentAnswer sa, Dictionary<int, Question> questionDict)
    {
        var qa = sa.QuestionAnswer;
        if (qa == null) return null;

        // Ưu tiên lấy question từ navigation property đã load, nếu không có mới tra cứu dict
        var question = qa.Question;
        if (question == null)
        {
            if (questionDict.TryGetValue(qa.QuestionId, out var dictQuestion)) question = dictQuestion;
        }

        if (question == null) return null;

        return new AnswerResult(
            question.QuestionId,
            question.QuestionContent,
            question.Chapter?.ChapterId ?? 0,
            question.Chapter?.Name ?? "N/A",
            question.Difficulty,
            CheckIsCorrect(qa, sa));
    }

    public static void GenerateTeacherRecommendations(ExamAnalyticsDetailDto dto)
    {
        if (dto.TotalSubmissions == 0) return;

        var weakChapters = dto.ChapterStats?.Where(c => c.Status == "Báo động").ToList() ?? new();
        var mediumChapters = dto.ChapterStats?.Where(c => c.Status == "Cần chú ý").ToList() ?? new();
        var goodChapters = dto.ChapterStats?.Where(c => c.Status == "Tốt").ToList() ?? new();

        bool hasContent = false;

        if (weakChapters.Any())
        {
            hasContent = true;
            var weakest = weakChapters.OrderBy(c => c.AccuracyRate).First();
            dto.Recommendations.Add(
                $"🚨 CẢNH BÁO: Lớp đang hổng kiến thức nặng nhất ở chương [{weakest.ChapterName}] " +
                $"(tỉ lệ đúng chỉ {weakest.AccuracyRate}%). " +
                $"ĐỀ XUẤT: Giáo viên cần tổ chức ôn tập lại lý thuyết và công thức cơ bản của chương này.");

            foreach (var ch in weakChapters.Skip(1))
                dto.Recommendations.Add(
                    $"🚨 Chương [{ch.ChapterName}] cũng ở mức báo động ({ch.AccuracyRate}% đúng).");
        }

        foreach (var ch in mediumChapters)
        {
            hasContent = true;
            dto.Recommendations.Add($"⚠️ Chương [{ch.ChapterName}] cần cải thiện ({ch.AccuracyRate}% đúng).");
        }

        if (dto.HardestQuestions != null && dto.HardestQuestions.Any())
        {
            hasContent = true;
            var hardest = dto.HardestQuestions.First();
            dto.Recommendations.Add(
                $"📌 Câu hỏi khó nhất thuộc [{hardest.ChapterName}] (chỉ {hardest.AccuracyRate}% đúng). Hãy giảng lại phần này.");
        }

        if (!hasContent)
        {
            dto.Recommendations.Add("📊 Hệ thống đang ghi nhận dữ liệu nộp bài. Khi có đủ câu trả lời, AI sẽ đưa ra phân tích chi tiết cho từng chương.");
        }
    }
}
