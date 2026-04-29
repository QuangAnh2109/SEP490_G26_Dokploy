using Backend.DTOs;
using FluentValidation;

namespace Backend.Validators.AssignExam;

public class CreateAssignExamRequestValidator : AbstractValidator<CreateAssignExamRequest>
{
    public CreateAssignExamRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.");
            
        RuleFor(x => x.Duration)
            .NotEmpty()
            .GreaterThan(0)
            .WithMessage("Duration must be > 0.");
            
        RuleFor(x => x.MaxAttempts)
            .NotEmpty()
            .GreaterThan(0)
            .WithMessage("MaxAttempts must be > 0.");
            
        RuleFor(x => x.PaperCount)
            .NotEmpty()
            .GreaterThan(0)
            .WithMessage("PaperCount must be > 0.");
            
        RuleFor(x => x.GenerationMode)
            .NotEmpty()
            .WithMessage("GenerationMode is required.");
    }
}

public class UpdateExamInfoRequestValidator : AbstractValidator<UpdateExamInfoRequest>
{
    public UpdateExamInfoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.");
    }
}

public class SwapQuestionRequestValidator : AbstractValidator<SwapQuestionRequestDto>
{
    public SwapQuestionRequestValidator()
    {
        RuleFor(x => x.PaperId)
            .NotEmpty()
            .WithMessage("PaperId is required.");
            
        RuleFor(x => x.OldQuestionId)
            .NotEmpty()
            .WithMessage("OldQuestionId is required.");
            
        RuleFor(x => x.NewQuestionId)
            .NotEmpty()
            .WithMessage("NewQuestionId is required.");
    }
}
