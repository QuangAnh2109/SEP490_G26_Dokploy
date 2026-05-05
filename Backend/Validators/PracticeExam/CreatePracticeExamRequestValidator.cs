using Backend.DTOs.PracticeExam;
using FluentValidation;

namespace Backend.Validators.PracticeExam;

public class CreatePracticeExamRequestValidator : AbstractValidator<CreatePracticeExamRequest>
{
    public CreatePracticeExamRequestValidator()
    {
        RuleFor(x => x.ClassId)
            .NotNull()
            .GreaterThan(0);

        RuleFor(x => x.ChapterIds)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.TotalQuestions)
            .NotNull()
            .InclusiveBetween(5, 30);
    }
}
