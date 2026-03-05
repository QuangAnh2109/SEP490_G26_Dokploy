using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements;

public class SubmissionService : ISubmissionService
{
    private readonly ISubmissionRepository _submissionRepo;

    public SubmissionService(ISubmissionRepository submissionRepo)
    {
        _submissionRepo = submissionRepo;
    }

    public async Task<SubmitExamResponse> SubmitExamAsync(
        int studentId,
        SubmitExamRequest request,
        CancellationToken ct = default)
    {
        // ── 1. Tìm Submission đang InProgress ──────────────────────────
        var submission = await _submissionRepo.GetActiveSubmissionAsync(
            request.ExamId, studentId, ct);

        if (submission == null)
            throw new KeyNotFoundException(ErrorMessages.SubmissionNotFound);

        if (submission.Status != SubmissionStatus.InProgress)
            throw new InvalidOperationException(ErrorMessages.SubmissionAlreadySubmitted);

        var exam = submission.Paper.Exam;

        // ── 2. Kiểm tra thời gian (chỉ dùng DateTime.UtcNow) ──────────
        var now = DateTime.UtcNow;
        var startTime = submission.CreatedAtUtc;             // thời gian bắt đầu làm bài
        var deadline = startTime.AddMinutes(exam.Duration);  // hết giờ theo duration

        bool isLate = now > deadline || (exam.CloseAt.HasValue && now > exam.CloseAt.Value);

        if (isLate && !exam.AllowLateSubmission)
            throw new InvalidOperationException(ErrorMessages.SubmissionLateNotAllowed);

        // ── 3. Validate QuestionAnswerIds thuộc Paper ────────────────────
        var validIds = await _submissionRepo.GetValidQuestionAnswerIdsAsync(
            submission.PaperId, ct);

        foreach (var sa in request.StudentAnswers)
        {
            if (!validIds.Contains(sa.QuestionAnswerId))
                throw new ArgumentException(ErrorMessages.InvalidQuestionAnswer);
        }

        // ── 4. Xử lý StudentAnswers ────────────────────────────────────
        var existingAnswers = submission.StudentAnswers.ToList();

        // Tập hợp QuestionAnswerId mới từ request
        var incomingIds = new HashSet<int>(
            request.StudentAnswers.Select(sa => sa.QuestionAnswerId));

        // Map QuestionAnswerId → dto cho tra cứu nhanh
        var incomingMap = request.StudentAnswers
            .ToDictionary(sa => sa.QuestionAnswerId);

        // Map QuestionAnswerId → existing StudentAnswer
        var existingMap = existingAnswers
            .ToDictionary(sa => sa.QuestionAnswerId);

        // 4a. Xóa những bản ghi cũ không còn trong request (chỉ áp dụng MCQ)
        var toRemove = existingAnswers
            .Where(sa => !incomingIds.Contains(sa.QuestionAnswerId))
            .ToList();

        // 4b. Thêm bản ghi mới chưa tồn tại
        var toAdd = new List<StudentAnswer>();

        // 4c. Update bản ghi đã tồn tại (FillInBlank: cập nhật Response)
        foreach (var dto in request.StudentAnswers)
        {
            if (existingMap.TryGetValue(dto.QuestionAnswerId, out var existing))
            {
                // Đã tồn tại → update Response (chủ yếu cho FillInBlank)
                existing.Response = dto.Response;
            }
            else
            {
                // Chưa tồn tại → thêm mới
                toAdd.Add(new StudentAnswer
                {
                    SubmissionId = submission.SubmissionId,
                    QuestionAnswerId = dto.QuestionAnswerId,
                    Response = dto.Response
                });
            }
        }

        if (toRemove.Count > 0)
            _submissionRepo.RemoveStudentAnswers(toRemove);

        if (toAdd.Count > 0)
            _submissionRepo.AddStudentAnswers(toAdd);

        // ── 5. Cập nhật Submission ──────────────────────────────────────
        submission.Status = request.Submit ? 2 : 1;
        submission.UpdatedAtUtc = now;

        await _submissionRepo.SaveChangesAsync(ct);

        // ── 6. Return response ──────────────────────────────────────────
        return new SubmitExamResponse(
            submission.SubmissionId,
            now,
            isLate
        );
    }
}
