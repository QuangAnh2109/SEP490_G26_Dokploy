using Backend.DTOs.Auth;
using FluentValidation;

namespace Backend.Validators.Auth;

public class ResendOtpRequestValidator : AbstractValidator<ResendOtpRequest>
{
    public ResendOtpRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
