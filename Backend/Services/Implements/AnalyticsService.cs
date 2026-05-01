using Backend.Constants;
using Backend.DTOs.Analytics;
using Backend.Common;
using Backend.Common.Models;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Backend.Common.Errors;

namespace Backend.Services.Implements;

public class AnalyticsService(IAnalyticsRepository analyticsRepo, IStudentExamRepository studentExamRepo, TimeProvider timeProvider) : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepo = analyticsRepo;
    private readonly IStudentExamRepository _studentExamRepo = studentExamRepo;
    private readonly TimeProvider _timeProvider = timeProvider;

    // ════════════════════════════════════════════════════════
    //  GIÁO VIÊN — Phân tích chi tiết bài thi
    // ════════════════════════════════════════════════════════
    public async Task<Result<ExamAnalyticsDetailDto>> GetExamAnalyticsDetailAsync(int examId)
    {
        var exam = await _analyticsRepo.GetExamWithFullGraphAsync(examId);
        if (exam == null)
            return AnalyticsErrors.ExamNotFound;

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
                SubmittedAt = DateTime.SpecifyKind(s.UpdatedAtUtc, DateTimeKind.Utc)
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
    public async Task<Result<StudentSubmissionAnalyticsDto>> GetStudentSubmissionAnalyticsAsync(int examId, int studentId)
    {
        var exam = await _analyticsRepo.GetExamWithFullGraphAsync(examId);
        if (exam == null)
            return AnalyticsErrors.ExamNotFound;

        // Lấy lượt làm bài mới nhất của học sinh này
        var submission = exam.Papers
            .SelectMany(p => p.Submissions)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefault();

        if (submission == null)
            return AnalyticsErrors.SubmissionNotFound;

        return await BuildSubmissionAnalyticsDtoAsync(exam, submission, exam.ShowScore, exam.ShowAnswer);
    }

    // ════════════════════════════════════════════════════════
    //  GIÁO VIÊN — Xem chi tiết bài làm theo submissionId
    // ════════════════════════════════════════════════════════
    public async Task<Result<StudentSubmissionAnalyticsDto>> GetSubmissionBySubmissionIdAsync(int submissionId)
    {
        var submission = await _analyticsRepo.GetSubmissionByIdWithPaperAsync(submissionId);
        if (submission?.Paper == null)
            return AnalyticsErrors.SubmissionNotFound;

        var examId = submission.Paper.ExamId ?? 0;
        var studentId = submission.StudentId;

        var exam = await _analyticsRepo.GetExamWithFullGraphAsync(examId);
        if (exam == null)
            return AnalyticsErrors.ExamNotFound;

        var targetSubmission = exam.Papers
            .SelectMany(p => p.Submissions)
            .FirstOrDefault(s => s.SubmissionId == submissionId);
        if (targetSubmission == null)
            return AnalyticsErrors.SubmissionNotFound;

        // Giáo viên luôn xem được điểm và đáp án
        var dto = await BuildSubmissionAnalyticsDtoAsync(exam, targetSubmission, showScore: 1, showAnswer: 2);
        return dto;
    }

    private async Task<StudentSubmissionAnalyticsDto> BuildSubmissionAnalyticsDtoAsync(Exam exam, Submission submission, int showScore, int showAnswer)
    {
        var dto = new StudentSubmissionAnalyticsDto
        {
            ShowScore = showScore,
            ShowAnswer = showAnswer,
            ExamId = exam.ExamId,
            ExamTitle = exam.Title,
            SubmissionId = submission.SubmissionId
        };

        var paper = exam.Papers.FirstOrDefault(p => p.PaperId == submission.PaperId);
        var paperQuestions = paper?.Questions?.ToList() ?? new List<Question>();
        dto.TotalQuestions = paperQuestions.DistinctBy(q => q.QuestionId).Count();

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
                    IsCorrect = showAnswer != 0 ? qa.IsCorrect : null,
                    CorrectAnswer = showAnswer != 0 ? qa.CorrectAnswer : null
                });
                if (sa != null) { if (!AnalyticsHelper.CheckIsCorrect(qa, sa)) questionCorrect = false; }
                else if (qa.IsCorrect == true) questionCorrect = false;
            }
            if (questionCorrect) correctCount++; else wrongCount++;
            answerAnalysis.Add((question.Chapter?.Name ?? "N/A", question.Difficulty, questionCorrect));
            dto.AnswerReview.Add(review);
        }

        if (showScore != 0)
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
            foreach (var stat in dto.ChapterStats!)
            {
                if (stat.AccuracyRate < 40)
                    dto.Recommendations.Add($"🚨 Cần ôn lại chương [{stat.ChapterName}] — tỉ lệ đúng chỉ {stat.AccuracyRate}%.");
                else if (stat.AccuracyRate < 70)
                    dto.Recommendations.Add($"⚠️ Chương [{stat.ChapterName}] cần luyện thêm ({stat.AccuracyRate}% đúng).");
                else
                    dto.Recommendations.Add($"🌟 Làm tốt chương [{stat.ChapterName}] ({stat.AccuracyRate}% đúng).");
            }
        }

        return await Task.FromResult(dto);
    }

    // ════════════════════════════════════════════════════════
    //  GIÁO VIÊN — Thống kê nộp bài (danh sách học sinh + lịch sử)
    // ════════════════════════════════════════════════════════
    public async Task<Result<ExamSubmitResultsDto>> GetExamSubmitResultsAsync(int examId)
    {
        await _studentExamRepo.ForceSubmitOverdueExamsAsync(examId);
        var exam = await _analyticsRepo.GetExamWithFullGraphAsync(examId);
        if (exam == null)
            return AnalyticsErrors.ExamNotFound;

        var rawSubmissions = exam.Papers.SelectMany(p => p.Submissions).ToList();
        var maxAttempts = exam.MaxAttempts > 0 ? exam.MaxAttempts : 999;

        // Lấy danh sách học sinh trong lớp (nếu có)
        var studentIdsInClass = new HashSet<int>();
        var studentDict = new Dictionary<int, User>();
        string? className = null;

        if (exam.ClassId.HasValue)
        {
            var members = await _analyticsRepo.GetClassMembersWithStudentsAsync(exam.ClassId.Value);
            foreach (var m in members)
            {
                studentIdsInClass.Add(m.StudentId);
                if (m.Student != null)
                    studentDict[m.StudentId] = m.Student;
            }
            className = exam.Class?.Name;
        }

        // Nếu không có lớp, lấy học sinh từ submissions
        if (studentIdsInClass.Count == 0)
        {
            foreach (var s in rawSubmissions)
            {
                studentIdsInClass.Add(s.StudentId);
                if (s.Student != null)
                    studentDict[s.StudentId] = s.Student;
            }
        }
        else
        {
            // Bổ sung thông tin từ submissions cho HS có trong lớp
            foreach (var s in rawSubmissions.Where(s => s.Student != null))
                studentDict.TryAdd(s.StudentId, s.Student!);
        }

        var submissionsByStudent = rawSubmissions
            .GroupBy(s => s.StudentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.CreatedAtUtc).ToList());

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var students = new List<StudentSubmitItemDto>();
        foreach (var studentId in studentIdsInClass.OrderBy(x => x))
        {
            var user = studentDict.GetValueOrDefault(studentId);
            var subs = submissionsByStudent.GetValueOrDefault(studentId) ?? new List<Submission>();

            var lastSub = subs.OrderByDescending(s => s.UpdatedAtUtc).FirstOrDefault();
            var submittedCount = subs.Count(s => s.Status == SubmissionStatus.Submitted);
            var inProgressCount = subs.Count(s => s.Status == SubmissionStatus.InProgress);

            string status;
            if (submittedCount > 0)
                status = "Đã nộp";
            else if (inProgressCount > 0)
                status = "Đang làm";
            else
                status = "Vắng thi";

            var history = new List<SubmissionHistoryDto>();
            int attemptNum = 0;
            foreach (var sub in subs.OrderBy(s => s.CreatedAtUtc))
            {
                attemptNum++;
                var duration = sub.Status == SubmissionStatus.Submitted
                    ? (sub.UpdatedAtUtc - sub.CreatedAtUtc)
                    : (now - sub.CreatedAtUtc);
                history.Add(new SubmissionHistoryDto
                {
                    SubmissionId = sub.SubmissionId,
                    AttemptNumber = attemptNum,
                    SubmittedAt = DateTime.SpecifyKind(sub.UpdatedAtUtc, DateTimeKind.Utc),
                    DurationFormatted = FormatDuration(duration),
                    Score = sub.TotalPoints,
                    IsLast = sub == lastSub
                });
            }

            students.Add(new StudentSubmitItemDto
            {
                StudentId = studentId,
                StudentCode = user?.StudentId ?? $"#{studentId}",
                FullName = user?.FullName ?? user?.Email ?? $"Học sinh #{studentId}",
                LastSubmitAt = lastSub?.Status == SubmissionStatus.Submitted ? DateTime.SpecifyKind(lastSub.UpdatedAtUtc, DateTimeKind.Utc) : null,
                DurationFormatted = lastSub != null
                    ? FormatDuration(lastSub.Status == SubmissionStatus.Submitted
                        ? (lastSub.UpdatedAtUtc - lastSub.CreatedAtUtc)
                        : (now - lastSub.CreatedAtUtc))
                    : null,
                LastScore = lastSub?.TotalPoints,
                AttemptCount = subs.Count,
                Status = status,
                History = history
            });
        }

        var totalSubmitted = students.Count(s => s.Status == "Đã nộp");

        return new ExamSubmitResultsDto
        {
            ExamId = exam.ExamId,
            ExamTitle = exam.Title,
            ClassName = className,
            DurationMinutes = exam.Duration,
            MaxAttempts = maxAttempts,
            TotalStudents = students.Count,
            SubmittedCount = totalSubmitted,
            Students = students.OrderByDescending(s => s.LastSubmitAt ?? DateTime.MinValue).ToList()
        };
    }

    private static string FormatDuration(TimeSpan d)
    {
        var totalSec = (int)d.TotalSeconds;
        var min = totalSec / 60;
        var sec = totalSec % 60;
        return $"{min}p {sec}s";
    }
}
