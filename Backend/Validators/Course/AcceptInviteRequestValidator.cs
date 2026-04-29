using Backend.DTOs.Course;
using FluentValidation;

namespace Backend.Validators.Course;

public class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequestDTO>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
