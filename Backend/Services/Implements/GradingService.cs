using System.Globalization;
using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements;

public class GradingService(
    ISubmissionRepository submissionRepo,
    ILogger<GradingService> logger) : IGradingService
{
    private const decimal ExamMaxPoint = 10m;

    public async Task GradeSubmissionAsync(int submissionId)
    {
        var submission = await submissionRepo.GetSubmissionForGradingAsync(submissionId);
        if (submission == null)
        {
            logger.LogWarning("Grading skipped: Submission {SubmissionId} not found.", submissionId);
            return;
        }

        if (submission.GradingStatus == GradingStatus.Graded)
        {
            logger.LogInformation("Grading skipped: Submission {SubmissionId} already graded.", submissionId);
            return;
        }

        var paperQuestionCount = await submissionRepo.GetPaperQuestionCountAsync(submission.PaperId);
        if (paperQuestionCount <= 0)
        {
            await submissionRepo.MarkGradingFailedAsync(submissionId, "Paper has no questions.");
            logger.LogError("Grading failed: Paper {PaperId} of Submission {SubmissionId} has no questions.",
                submission.PaperId, submissionId);
            return;
        }

        try
        {
            await using var tx = await submissionRepo.BeginTransactionAsync();

            submission.GradingStatus = GradingStatus.InProgress;
            await submissionRepo.SaveChangesAsync();

            var questionPoint = ExamMaxPoint / paperQuestionCount;

            var byQuestion = submission.StudentAnswers
                .Where(sa => sa.QuestionAnswer != null && sa.QuestionAnswer.Question != null)
                .GroupBy(sa => sa.QuestionAnswer.Question);

            foreach (var group in byQuestion)
            {
                var question = group.Key;
                var answers = group.ToList();

                if (question.QuestionType == QuestionType.Mcq)
                {
                    GradeMcq(answers, questionPoint);
                }
                else if (question.QuestionType == QuestionType.FillBlank)
                {
                    GradeFillBlank(question, answers, questionPoint);
                }
                else
                {
                    logger.LogWarning(
                        "Unknown QuestionType {Type} on Question {QuestionId} (Submission {SubmissionId}); answers left at 0 points.",
                        question.QuestionType, question.QuestionId, submissionId);
                }
            }

            submission.TotalPoints = submission.StudentAnswers.Sum(sa => sa.PointsEarned ?? 0m);
            submission.GradingStatus = GradingStatus.Graded;
            submission.GradingError = null;
            await submissionRepo.SaveChangesAsync();

            await tx.CommitAsync();

            logger.LogInformation("Graded Submission {SubmissionId}: TotalPoints={TotalPoints}",
                submissionId, submission.TotalPoints);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Grading exception on Submission {SubmissionId}", submissionId);
            var msg = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            await submissionRepo.MarkGradingFailedAsync(submissionId, msg);
        }
    }

    private static void GradeMcq(List<StudentAnswer> answers, decimal questionPoint)
    {
        if (answers.Count == 0) return;

        var question = answers[0].QuestionAnswer.Question;
        var correctIds = question.QuestionAnswers
            .Where(qa => qa.IsCorrect == true)
            .Select(qa => qa.QuestionAnswerId)
            .ToHashSet();

        var chosenIds = answers.Select(sa => sa.QuestionAnswerId).ToHashSet();
        var passed = chosenIds.SetEquals(correctIds);

        if (passed)
        {
            var share = questionPoint / answers.Count;
            foreach (var sa in answers)
            {
                sa.IsCorrect = true;
                sa.PointsEarned = share;
            }
        }
        else
        {
            foreach (var sa in answers)
            {
                sa.IsCorrect = false;
                sa.PointsEarned = 0m;
            }
        }
    }

    private static void GradeFillBlank(Question question, List<StudentAnswer> answers, decimal questionPoint)
    {
        var allQas = question.QuestionAnswers.ToList();
        var blankCount = allQas.Count;
        if (blankCount == 0) return;

        var blankPoint = questionPoint / blankCount;

        foreach (var sa in answers)
        {
            sa.IsCorrect = TextEqual(sa.Response, sa.QuestionAnswer?.CorrectAnswer);
        }

        var answersByQaId = answers.ToDictionary(sa => sa.QuestionAnswerId);
        var hasGroup = allQas.Any(qa => qa.GroupAnswerId != null);

        if (!hasGroup)
        {
            var allCorrect = allQas.All(qa =>
                answersByQaId.TryGetValue(qa.QuestionAnswerId, out var sa) && sa.IsCorrect == true);

            foreach (var sa in answers)
            {
                sa.PointsEarned = allCorrect ? blankPoint : 0m;
            }
            return;
        }

        var groupPassed = allQas
            .Where(qa => qa.GroupAnswerId != null)
            .GroupBy(qa => qa.GroupAnswerId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.All(qa =>
                    answersByQaId.TryGetValue(qa.QuestionAnswerId, out var sa) && sa.IsCorrect == true));

        var groupEntities = allQas
            .Where(qa => qa.GroupAnswer != null)
            .Select(qa => qa.GroupAnswer!)
            .DistinctBy(g => g.GroupAnswerId)
            .ToDictionary(g => g.GroupAnswerId);

        var finalCache = new Dictionary<int, bool>();
        bool FinalPassed(int gId)
        {
            if (finalCache.TryGetValue(gId, out var cached)) return cached;
            var ok = groupPassed.TryGetValue(gId, out var passed) && passed;
            if (ok && groupEntities.TryGetValue(gId, out var ge) && ge.DependsOnGroupId.HasValue)
            {
                ok = FinalPassed(ge.DependsOnGroupId.Value);
            }
            finalCache[gId] = ok;
            return ok;
        }

        foreach (var sa in answers)
        {
            var gId = sa.QuestionAnswer?.GroupAnswerId;
            sa.PointsEarned = gId.HasValue && FinalPassed(gId.Value) ? blankPoint : 0m;
        }
    }

    private static bool TextEqual(string? response, string? correct)
    {
        if (response == null || correct == null) return false;

        var s = StripAllWhitespace(response.Trim());
        var c = StripAllWhitespace(correct.Trim());

        if (s.Length == 0 || c.Length == 0) return false;

        if (decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var ns)
            && decimal.TryParse(c, NumberStyles.Float, CultureInfo.InvariantCulture, out var nc))
        {
            return ns == nc;
        }

        return s.Equals(c, StringComparison.OrdinalIgnoreCase);
    }

    private static string StripAllWhitespace(string s)
    {
        return string.Concat(s.Where(ch => !char.IsWhiteSpace(ch)));
    }
}
