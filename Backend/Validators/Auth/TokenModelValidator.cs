using Backend.DTOs.Auth;
using FluentValidation;

namespace Backend.Validators.Auth;

public class TokenModelValidator : AbstractValidator<TokenModel>
{
    public TokenModelValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
