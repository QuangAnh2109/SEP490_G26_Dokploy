using Backend.DTOs.Class;
using FluentValidation;

namespace Backend.Validators.Class;

public class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequestDTO>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
