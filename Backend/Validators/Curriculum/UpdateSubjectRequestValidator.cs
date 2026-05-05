using Backend.DTOs.Curriculum.Subject;
using FluentValidation;

namespace Backend.Validators.Curriculum;

public class UpdateSubjectRequestValidator : AbstractValidator<UpdateSubjectRequest>
{
    public UpdateSubjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
            
        RuleFor(x => x.Description)
            .MaximumLength(1000);
            
        RuleFor(x => x.ConcurrencyStamp)
            .NotEmpty();
    }
}
