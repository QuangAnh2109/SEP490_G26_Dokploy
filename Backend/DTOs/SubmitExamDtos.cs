namespace Backend.DTOs;

// ── Request ──────────────────────────────────────────────

public class SubmitExamRequest
{
    public int? ExamId { get; set; }
    public List<StudentAnswerDto>? StudentAnswers { get; set; }

    public bool? Submit { get; set; }
}

public class StudentAnswerDto
{
    /// <summary>FK → QuestionAnswers – xác định học sinh đang trả lời đáp án / ô trống nào</summary>
    public int? QuestionAnswerId { get; set; }

    /// <summary>
    /// Với MultipleChoice: null (chỉ cần QuestionAnswerId để biết học sinh chọn đáp án nào).
    /// Với FillInBlank: nội dung học sinh điền vào ô trống.
    /// </summary>
    public string? Response { get; set; }
}

// ── Response ─────────────────────────────────────────────

public record SubmitExamResponse(
    int SubmissionId,
    DateTime SubmittedAtUtc,
    bool IsLate
);
