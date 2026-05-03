using Backend.DTOs.Curriculum.Subject;
using FluentValidation;

namespace Backend.Validators.Curriculum;

public class CreateSubjectRequestValidator : AbstractValidator<CreateSubjectRequest>
{
    public CreateSubjectRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Z0-9_]+$").WithMessage("Code must only contain uppercase letters, numbers, and underscores.");
            
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
            
        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}
