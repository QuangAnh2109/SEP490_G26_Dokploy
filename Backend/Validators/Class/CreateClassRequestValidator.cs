using Backend.DTOs.Class;
using FluentValidation;

namespace Backend.Validators.Class;

public class CreateClassRequestValidator : AbstractValidator<CreateClassRequestDTO>
{
    public CreateClassRequestValidator()
    {
        RuleFor(x => x.ClassName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SubjectId).NotNull().GreaterThan(0);
        RuleFor(x => x.SemesterId).NotNull().GreaterThan(0);
    }
}
