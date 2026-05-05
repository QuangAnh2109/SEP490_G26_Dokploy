using Backend.DTOs;
using FluentValidation;

namespace Backend.Validators.AssignExam;

public class CreateAssignExamRequestValidator : AbstractValidator<CreateAssignExamRequest>
{
    public CreateAssignExamRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty();

        RuleFor(x => x.Duration)
            .NotNull()
            .GreaterThan(0);

        RuleFor(x => x.MaxAttempts)
            .NotNull()
            .GreaterThan(0);

        RuleFor(x => x.PaperCount)
            .NotNull()
            .GreaterThan(0);

        RuleFor(x => x.GenerationMode)
            .NotEmpty();
    }
}

public class UpdateExamInfoRequestValidator : AbstractValidator<UpdateExamInfoRequest>
{
    public UpdateExamInfoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty();
    }
}

public class SwapQuestionRequestValidator : AbstractValidator<SwapQuestionRequestDto>
{
    public SwapQuestionRequestValidator()
    {
        RuleFor(x => x.PaperId)
            .NotNull()
            .GreaterThan(0);

        RuleFor(x => x.OldQuestionId)
            .NotNull()
            .GreaterThan(0);

        RuleFor(x => x.NewQuestionId)
            .NotNull()
            .GreaterThan(0);
    }
}
