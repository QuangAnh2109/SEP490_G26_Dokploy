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
            .Must(t => QuestionType.IsValid(t ?? string.Empty));

        RuleFor(x => x.Stem)
            .NotEmpty();

        RuleFor(x => x.Difficulty)
            .NotNull()
            .Must(d => DifficultyLevel.IsValid(d ?? 0));

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => QuestionStatus.IsValid(s ?? string.Empty));

        RuleFor(x => x.QuestionPurpose)
            .NotNull()
            .Must(p => Constants.QuestionPurpose.IsValid(p ?? 0));

        RuleFor(x => x.ChapterId)
            .NotNull()
            .MustAsync(async (id, cancellation) => id.HasValue && await questionRepository.ChapterExistsAsync(id.Value));

        RuleFor(x => x.Answers)
            .NotEmpty();

        When(x => x.QuestionType == QuestionType.FillBlank, () =>
        {
            RuleFor(x => x.Frame).NotEmpty();
            RuleForEach(x => x.Answers).SetValidator(new FillBlankAnswerDtoValidator());
        }).Otherwise(() =>
        {
            RuleFor(x => x.Answers)
                .Must(a => a != null && a.Count >= 2)
                .Must(a => a != null && a.Any(ans => ans.IsCorrect == true));
            RuleForEach(x => x.Answers).SetValidator(new MultipleChoiceAnswerDtoValidator());
        });

        RuleFor(x => x.Answers)
            .Must(a => a != null && a.Sum(ans => ans.Point ?? 0) == RequiredTotalPoint)
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
            .NotNull().GreaterThan(0);

        RuleFor(x => x.Point)
            .NotNull().InclusiveBetween(MinPoint, MaxPoint);
    }
}

public class MultipleChoiceAnswerDtoValidator : AbstractValidator<AnswerDto>
{
    private const int MinPoint = 0;
    private const int MaxPoint = 100;

    public MultipleChoiceAnswerDtoValidator()
    {
        RuleFor(x => x.Point)
            .NotNull().InclusiveBetween(MinPoint, MaxPoint);
    }
}
