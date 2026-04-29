using Backend.DTOs.Course;
using FluentValidation;

namespace Backend.Validators.Course;

public class InviteStudentRequestValidator : AbstractValidator<InviteStudentRequestDTO>
{
    public InviteStudentRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
