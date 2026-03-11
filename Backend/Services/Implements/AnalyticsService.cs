using Backend.Constants;
using Backend.DTOs.Analytics;
using Backend.Helper;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Services.Implements;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepo;

    public AnalyticsService(IAnalyticsRepository analyticsRepo)
    {
        _analyticsRepo = analyticsRepo;
    }

    // ════════════════════════════════════════════════════════
    //  GIÁO VIÊN — Phân tích chi tiết bài thi
    // ════════════════════════════════════════════════════════
    public async Task<ExamAnalyticsDetailDto> GetExamAnalyticsDetailAsync(int examId)
    {
        var exam = await _analyticsRepo.GetExamWithFullGraphAsync(examId);
        if (exam == null)
            throw new KeyNotFoundException($"Không tìm thấy bài thi với ID {examId}.");

        var rawSubmissions = exam.Papers.SelectMany(p => p.Submissions).ToList();
        
        // Lọc lấy lượt nộp mới nhất MÀ CÓ câu trả lời của mỗi học sinh
        // Tránh trường hợp nộp bài trống làm rỗng biểu đồ
        var allSubmissions = rawSubmissions
            .Where(s => s.StudentAnswers != null && s.StudentAnswers.Any())
            .GroupBy(s => s.StudentId)
            .Select(g => g.OrderByDescending(s => s.UpdatedAtUtc).First())
            .ToList();

        var dto = new ExamAnalyticsDetailDto
        {
            ExamId = exam.ExamId,
            ExamTitle = exam.Title,
            TotalSubmissions = rawSubmissions.Count // Vẫn giữ tổng số lượt nộp thực tế
        };

        if (dto.TotalSubmissions == 0)
        {
            dto.Recommendations.Add("Chưa có học sinh nào nộp bài thi này để phân tích.");
            return dto;
        }

        // ── 1. Thống kê điểm ──
        var scores = allSubmissions
            .Where(s => s.TotalPoints.HasValue)
            .Select(s => s.TotalPoints!.Value)
            .OrderBy(s => s)
            .ToList();

        if (scores.Count > 0)
        {
            dto.AverageScore = Math.Round(scores.Average(), 2);
            dto.MaxScore = scores.Max();
            dto.MinScore = scores.Min();
            dto.MedianScore = AnalyticsHelper.GetMedian(scores);
        }

        // ── 2. Phân bố điểm ──
        dto.ScoreDistribution = AnalyticsHelper.BuildScoreDistribution(scores);

        // ── 3. Build question lookup (Exhaustive) ──
        // Lấy tất cả câu hỏi từ các Paper
        var paperQuestions = exam.Papers.SelectMany(p => p.Questions).DistinctBy(q => q.QuestionId).ToList();
        
        // Bổ sung các câu hỏi từ các câu trả lời học sinh nộp (trong trường hợp quan hệ Paper-Question bị gãy/không load đủ)
        var submissionQuestions = allSubmissions
            .SelectMany(s => s.StudentAnswers)
            .Select(sa => sa.QuestionAnswer?.Question)
            .Where(q => q != null)
            .DistinctBy(q => q!.QuestionId)
            .Select(q => q!)
            .ToList();

        var allQuestions = paperQuestions.UnionBy(submissionQuestions, q => q.QuestionId).ToList();
        var questionDict = allQuestions.ToDictionary(q => q.QuestionId, q => q);

        // ── 4. Tính tỉ lệ đúng ──
        var allAnswerResults = allSubmissions
            .SelectMany(s => s.StudentAnswers)
            .Select(ans => AnalyticsHelper.MapStudentAnswer(ans, questionDict))
            .Where(x => x != null)
            .ToList();

        // ── 5. Thống kê theo Chương ──
        dto.ChapterStats = allAnswerResults
            .Where(x => x != null)
            .GroupBy(x => x!.ChapterName) // Nhóm theo tên chương cho trực quan
            .Select(g => new ChapterAnalyticsDto
            {
                ChapterName = g.Key,
                TotalAnswers = g.Count(),
                CorrectAnswers = g.Count(x => x!.IsCorrect)
            })
            .OrderBy(c => c.AccuracyRate)
            .ToList();

        // ── 6. Thống kê theo Độ khó ──
        dto.DifficultyStats = allAnswerResults
            .Where(x => x != null)
            .GroupBy(x => x!.Difficulty)
            .Select(g => new DifficultyAnalyticsDto
            {
                DifficultyLevel = g.Key,
                DifficultyName = DifficultyLevel.GetLabel(g.Key),
                TotalAnswers = g.Count(),
                CorrectAnswers = g.Count(x => x!.IsCorrect)
            })
            .OrderBy(d => d.DifficultyLevel)
            .ToList();

        // ── 7. Top câu hỏi khó nhất ──
        dto.HardestQuestions = allAnswerResults
            .GroupBy(x => x!.QuestionId)
            .Select(g =>
            {
                var first = g.First()!;
                return new HardestQuestionDto
                {
                    QuestionId = g.Key,
                    QuestionContent = first.QuestionContent,
                    ChapterName = first.ChapterName,
                    Difficulty = first.Difficulty,
                    DifficultyName = DifficultyLevel.GetLabel(first.Difficulty),
                    TotalAttempts = g.Count(),
                    CorrectCount = g.Count(x => x!.IsCorrect)
                };
            })
            .OrderBy(x => x.AccuracyRate)
            .Take(10)
            .ToList();

        // ── 8. Danh sách sinh viên ──
        dto.StudentResults = allSubmissions
            .Select(s => new StudentResultDto
            {
                StudentId = s.StudentId,
                StudentName = s.Student?.FullName ?? s.Student?.Email ?? $"HS #{s.StudentId}",
                TotalPoints = s.TotalPoints,
                SubmittedAt = s.UpdatedAtUtc
            })
            .OrderByDescending(s => s.TotalPoints)
            .ToList();

        // ── 9. Đề xuất cải thiện ──
        AnalyticsHelper.GenerateTeacherRecommendations(dto);

        // ── 10. Debug Info ──
        dto.DebugInfo = new
        {
            PaperCount = exam.Papers.Count,
            RawSubmissionsCount = rawSubmissions.Count,
            ValidSubmissionsCount = allSubmissions.Count,
            TotalAnswersFound = allSubmissions.SelectMany(s => s.StudentAnswers).Count(),
            AllAnswerResultsCount = allAnswerResults.Count,
            QuestionDictCount = questionDict.Count
        };

        return dto;
    }

    // ════════════════════════════════════════════════════════
    //  HỌC SINH — Phân tích bài làm cá nhân
    // ════════════════════════════════════════════════════════
    public async Task<StudentSubmissionAnalyticsDto> GetStudentSubmissionAnalyticsAsync(int examId, int studentId)
    {
        var exam = await _analyticsRepo.GetExamWithFullGraphAsync(examId);
        if (exam == null)
            throw new KeyNotFoundException($"Không tìm thấy bài thi với ID {examId}.");

        // Lấy lượt làm bài mới nhất của học sinh này
        var submission = exam.Papers
            .SelectMany(p => p.Submissions)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefault();

        if (submission == null)
            throw new KeyNotFoundException($"Không tìm thấy bài làm của học sinh {studentId} cho bài thi {examId}.");

        var showScore = exam.ShowScore;
        var showAnswer = exam.ShowAnswer;

        var dto = new StudentSubmissionAnalyticsDto
        {
            ShowScore = showScore,
            ShowAnswer = showAnswer,
            ExamId = exam.ExamId,
            ExamTitle = exam.Title,
            SubmissionId = submission.SubmissionId
        };

        // ── Build question lookup ──
        var paper = exam.Papers.FirstOrDefault(p => p.PaperId == submission.PaperId);
        var paperQuestions = paper?.Questions?.ToList() ?? new List<Question>();
        dto.TotalQuestions = paperQuestions.DistinctBy(q => q.QuestionId).Count();

        // ── Xem lại bài làm + tính đúng/sai nội bộ ──
        int questionOrder = 0, correctCount = 0, wrongCount = 0;
        var answerAnalysis = new List<(string ChapterName, int Difficulty, bool IsCorrect)>();

        foreach (var question in paperQuestions.DistinctBy(q => q.QuestionId))
        {
            questionOrder++;
            var review = new AnswerReviewDto
            {
                QuestionId = question.QuestionId,
                QuestionOrder = questionOrder,
                QuestionContent = question.QuestionContent,
                QuestionType = question.QuestionType,
                ChapterName = question.Chapter?.Name ?? "N/A"
            };

            bool questionCorrect = true;

            foreach (var qa in question.QuestionAnswers)
            {
                var sa = submission.StudentAnswers.FirstOrDefault(a => a.QuestionAnswerId == qa.QuestionAnswerId);

                review.Options.Add(new AnswerOptionReviewDto
                {
                    QuestionAnswerId = qa.QuestionAnswerId,
                    Content = qa.Content,
                    StudentResponse = sa?.Response,
                    IsSelected = sa != null,
                    IsCorrect = showAnswer ? qa.IsCorrect : null,
                    CorrectAnswer = showAnswer ? qa.CorrectAnswer : null
                });

                if (sa != null) { if (!AnalyticsHelper.CheckIsCorrect(qa, sa)) questionCorrect = false; }
                else if (qa.IsCorrect == true) questionCorrect = false;
            }

            if (questionCorrect) correctCount++; else wrongCount++;
            answerAnalysis.Add((question.Chapter?.Name ?? "N/A", question.Difficulty, questionCorrect));
            dto.AnswerReview.Add(review);
        }

        // ── Phần chỉ hiện khi ShowScore = true ──
        if (showScore)
        {
            dto.TotalPoints = submission.TotalPoints;
            dto.CorrectCount = correctCount;
            dto.WrongCount = wrongCount;

            var classScores = exam.Papers.SelectMany(p => p.Submissions)
                .Where(s => s.TotalPoints.HasValue).Select(s => s.TotalPoints!.Value).ToList();
            if (classScores.Count > 0)
            {
                dto.ClassAverageScore = Math.Round(classScores.Average(), 2);
                dto.ClassMaxScore = classScores.Max();
            }

            dto.ChapterStats = answerAnalysis.GroupBy(x => x.ChapterName)
                .Select(g => new ChapterAnalyticsDto { ChapterName = g.Key, TotalAnswers = g.Count(), CorrectAnswers = g.Count(x => x.IsCorrect) })
                .OrderBy(c => c.AccuracyRate).ToList();

            dto.DifficultyStats = answerAnalysis.GroupBy(x => x.Difficulty)
                .Select(g => new DifficultyAnalyticsDto { DifficultyLevel = g.Key, DifficultyName = DifficultyLevel.GetLabel(g.Key), TotalAnswers = g.Count(), CorrectAnswers = g.Count(x => x.IsCorrect) })
                .OrderBy(d => d.DifficultyLevel).ToList();

            dto.Recommendations = new List<string>();
            foreach (var stat in dto.ChapterStats)
            {
                if (stat.AccuracyRate < 40)
                    dto.Recommendations.Add($"🚨 Em cần ôn lại chương [{stat.ChapterName}] — tỉ lệ đúng chỉ {stat.AccuracyRate}%. Hãy xem lại lý thuyết cơ bản và làm lại các bài tập mẫu.");
                else if (stat.AccuracyRate < 70)
                    dto.Recommendations.Add($"⚠️ Chương [{stat.ChapterName}] cần luyện thêm ({stat.AccuracyRate}% đúng). Thử làm thêm bài tập để cải thiện.");
                else
                    dto.Recommendations.Add($"🌟 Em làm tốt chương [{stat.ChapterName}] ({stat.AccuracyRate}% đúng). Hãy thử thách với bài tập nâng cao!");
            }
        }

        return dto;
    }
}
