using Backend.DTOs.Class;
using FluentValidation;

namespace Backend.Validators.Class;

public class JoinClassRequestValidator : AbstractValidator<JoinClassRequestDTO>
{
    public JoinClassRequestValidator()
    {
        RuleFor(x => x.InvitationCode).NotEmpty();
    }
}
