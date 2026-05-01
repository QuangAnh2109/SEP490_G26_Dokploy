using Backend.DTOs.Admin;
using FluentValidation;

namespace Backend.Validators.Admin;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.RoleId)
            .NotNull()
            .Must(r => r == 1 || r == 2)
            .WithMessage("RoleId must be 1 (Teacher) or 2 (Student).");

        RuleFor(x => x.StudentId)
            .Matches(@"^[A-Za-z]{2}\d{6}$")
            .When(x => x.RoleId == 2 && !string.IsNullOrWhiteSpace(x.StudentId));
    }
}
