using Backend.DTOs.Class;
using FluentValidation;

namespace Backend.Validators.Class;

public class InviteStudentRequestValidator : AbstractValidator<InviteStudentRequestDTO>
{
    public InviteStudentRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
