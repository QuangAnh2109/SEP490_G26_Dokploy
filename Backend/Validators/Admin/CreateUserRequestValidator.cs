using Backend.Common;
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
            .MinimumLength(2)
            .MaximumLength(200);

        RuleFor(x => x.RoleId)
            .NotNull()
            .Must(r => r == RoleIds.TeacherInt || r == RoleIds.StudentInt);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .Matches(@"^0\d{9}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        When(x => x.RoleId == RoleIds.StudentInt, () =>
        {
            RuleFor(x => x.StudentId)
                .NotEmpty()
                .MaximumLength(20)
                .Matches(@"^[A-Za-z]{2}\d{6}$");
        });

        When(x => x.RoleId == RoleIds.TeacherInt, () =>
        {
            RuleFor(x => x.StudentId).Empty();
        });
    }
}
