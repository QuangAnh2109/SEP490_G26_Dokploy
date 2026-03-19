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
        // 1. Fetch Exam Info (Basic)
        var exam = await _analyticsRepo.GetExamWithFullGraphAsync(examId); // Actually just need basic info now
        if (exam == null)
            throw new KeyNotFoundException($"Không tìm thấy bài thi với ID {examId}.");

        // 2. Fetch Aggregated stats from SQL
        var studentSummaries = await _analyticsRepo.GetExamSubmissionsSummaryAsync(examId);
        var chapterStatsQueries = await _analyticsRepo.GetChapterStatsAsync(examId);
        var difficultyStatsQueries = await _analyticsRepo.GetDifficultyStatsAsync(examId);
        var hardestQuestionsQueries = await _analyticsRepo.GetHardestQuestionsAsync(examId, 10);

        var dto = new ExamAnalyticsDetailDto
        {
            ExamId = exam.ExamId,
            ExamTitle = exam.Title,
            TotalSubmissions = studentSummaries.Count
        };

        if (dto.TotalSubmissions == 0)
        {
            dto.Recommendations.Add("Chưa có học sinh nào nộp bài thi này để phân tích.");
            return dto;
        }

        // 3. Score Stats
        var scores = studentSummaries
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
            dto.ScoreDistribution = AnalyticsHelper.BuildScoreDistribution(scores);
        }

        // 4. Map SQL Stats to DTO
        dto.ChapterStats = chapterStatsQueries.Select(c => new ChapterAnalyticsDto
        {
            ChapterName = c.ChapterName,
            TotalAnswers = c.TotalAnswers,
            CorrectAnswers = c.CorrectAnswers
        }).ToList();

        dto.DifficultyStats = difficultyStatsQueries.Select(d => new DifficultyAnalyticsDto
        {
            DifficultyLevel = d.Difficulty,
            DifficultyName = DifficultyLevel.GetLabel(d.Difficulty),
            TotalAnswers = d.TotalAnswers,
            CorrectAnswers = d.CorrectAnswers
        }).ToList();

        dto.HardestQuestions = hardestQuestionsQueries.Select(q => new HardestQuestionDto
        {
            QuestionId = q.QuestionId,
            QuestionContent = q.QuestionContent,
            ChapterName = q.ChapterName,
            Difficulty = q.Difficulty,
            DifficultyName = DifficultyLevel.GetLabel(q.Difficulty),
            TotalAttempts = q.TotalAttempts,
            CorrectCount = q.CorrectCount
        }).ToList();

        dto.StudentResults = studentSummaries.Select(s => new StudentResultDto
        {
            StudentId = s.StudentId,
            StudentName = s.StudentName ?? $"HS #{s.StudentId}",
            TotalPoints = s.TotalPoints,
            SubmittedAt = s.UpdatedAtUtc
        }).ToList();

        // 5. Recommendations
        AnalyticsHelper.GenerateTeacherRecommendations(dto);

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

        var scoreMode = exam.ShowScore;
        var answerMode = exam.ShowAnswer;
        var answerTiming = exam.AnswerTimingMode;
        var showScore = scoreMode != 0;  // 0 = none
        var showAnswer = answerMode != 0; // 0 = none

        var dto = new StudentSubmissionAnalyticsDto
        {
            ShowScore = scoreMode,
            ShowAnswer = answerMode,
            AnswerTimingMode = answerTiming,
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
                    IsCorrect = answerMode == 2 ? qa.IsCorrect : null,  // 2 = with_correct
                    CorrectAnswer = answerMode == 2 ? qa.CorrectAnswer : null
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
