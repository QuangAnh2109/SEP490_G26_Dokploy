using Backend.Constants;
using Backend.DTOs.Question;
using Backend.Repositories.Interfaces;
using FluentValidation;

namespace Backend.Validators.Question;

public class QuestionDtoValidator : AbstractValidator<QuestionDto>
{
    private const int MinPoint = 0;
    private const int MaxPoint = 100;
    private const int RequiredTotalPoint = 100;

    public QuestionDtoValidator(IQuestionRepository questionRepository)
    {
        RuleFor(x => x.QuestionType)
            .NotEmpty()
            .Must(t => QuestionType.IsValid(t ?? string.Empty)).WithMessage("Loại câu hỏi không hợp lệ.");

        RuleFor(x => x.Stem)
            .NotEmpty().WithMessage("Đề bài không được để trống.");

        RuleFor(x => x.Difficulty)
            .NotNull()
            .Must(d => DifficultyLevel.IsValid(d ?? 0)).WithMessage("Mức độ phải từ 1 đến 4.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => QuestionStatus.IsValid(s ?? string.Empty)).WithMessage("Trạng thái không hợp lệ.");

        RuleFor(x => x.QuestionPurpose)
            .NotNull()
            .Must(p => Constants.QuestionPurpose.IsValid(p ?? 0)).WithMessage("Mục đích câu hỏi không hợp lệ (1=Kiểm tra, 2=Luyện tập).");

        RuleFor(x => x.ChapterId)
            .NotNull()
            .MustAsync(async (id, cancellation) => id.HasValue && await questionRepository.ChapterExistsAsync(id.Value))
            .WithMessage("Chương không tồn tại.");

        RuleFor(x => x.Answers)
            .NotEmpty().WithMessage("Phải có ít nhất 1 đáp án.");

        When(x => x.QuestionType == QuestionType.FillBlank, () =>
        {
            RuleFor(x => x.Frame).NotEmpty().WithMessage("Khung trả lời không được để trống.");
            RuleForEach(x => x.Answers).SetValidator(new FillBlankAnswerDtoValidator());
        }).Otherwise(() =>
        {
            RuleFor(x => x.Answers)
                .Must(a => a != null && a.Count >= 2).WithMessage("Câu trắc nghiệm phải có ít nhất 2 lựa chọn.")
                .Must(a => a != null && a.Any(ans => ans.IsCorrect == true)).WithMessage("Phải có ít nhất 1 đáp án đúng.");
            RuleForEach(x => x.Answers).SetValidator(new MultipleChoiceAnswerDtoValidator());
        });

        RuleFor(x => x.Answers)
            .Must(a => a != null && a.Sum(ans => ans.Point ?? 0) == RequiredTotalPoint)
            .WithMessage($"Tổng điểm phải bằng {RequiredTotalPoint}%.")
            .When(x => x.Answers != null && x.Answers.Any());
    }
}

public class FillBlankAnswerDtoValidator : AbstractValidator<AnswerDto>
{
    private const int MinPoint = 0;
    private const int MaxPoint = 100;

    public FillBlankAnswerDtoValidator()
    {
        RuleFor(x => x.InputTypeId)
            .NotNull().GreaterThan(0).WithMessage("Thiếu loại giới hạn nhập liệu.");

        RuleFor(x => x.Point)
            .NotNull().InclusiveBetween(MinPoint, MaxPoint).WithMessage($"Điểm phải từ {MinPoint} đến {MaxPoint}.");
    }
}

public class MultipleChoiceAnswerDtoValidator : AbstractValidator<AnswerDto>
{
    private const int MinPoint = 0;
    private const int MaxPoint = 100;

    public MultipleChoiceAnswerDtoValidator()
    {
        RuleFor(x => x.Point)
            .NotNull().InclusiveBetween(MinPoint, MaxPoint).WithMessage($"Điểm phải từ {MinPoint} đến {MaxPoint}.");
    }
}
