using Backend.DTOs.Auth;
using FluentValidation;

namespace Backend.Validators.Auth;

public class ChangePasswordFirstLoginRequestValidator : AbstractValidator<ChangePasswordFirstLoginRequest>
{
    public ChangePasswordFirstLoginRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$");
    }
}
