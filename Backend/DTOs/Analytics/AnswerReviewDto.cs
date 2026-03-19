namespace Backend.DTOs.Analytics;

/// <summary>
/// DTO cho học sinh xem lại từng câu trả lời.
/// Luôn trả về câu hỏi + câu trả lời đã điền.
/// Đáp án đúng chỉ trả về khi Exam.ShowAnswer = true.
/// </summary>
public class AnswerReviewDto
{
    public int QuestionId { get; set; }
    public int QuestionOrder { get; set; }
    public string QuestionContent { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public string ChapterName { get; set; } = null!;

    /// <summary>
    /// Danh sách đáp án/ô trống của câu hỏi, kèm response của HS
    /// </summary>
    public List<AnswerOptionReviewDto> Options { get; set; } = new();
}

public class AnswerOptionReviewDto
{
    public int QuestionAnswerId { get; set; }

    /// <summary>Nội dung đáp án (A, B, C, D hoặc nội dung ô trống)</summary>
    public string Content { get; set; } = null!;

    /// <summary>Câu trả lời HS đã chọn/điền. Null nếu không chọn đáp án này.</summary>
    public string? StudentResponse { get; set; }

    /// <summary>HS có chọn đáp án này không (cho MultipleChoice)</summary>
    public bool IsSelected { get; set; }

    /// <summary>Đáp án này có đúng không — chỉ trả về khi ShowAnswer = true, otherwise null</summary>
    public bool? IsCorrect { get; set; }

    /// <summary>Đáp án đúng — chỉ trả về khi ShowAnswer = true, otherwise null</summary>
    public string? CorrectAnswer { get; set; }
}
