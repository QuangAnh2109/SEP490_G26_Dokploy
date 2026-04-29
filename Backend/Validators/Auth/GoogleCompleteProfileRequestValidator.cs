using Backend.DTOs;
using FluentValidation;

namespace Backend.Validators.Auth;

public class GoogleCompleteProfileRequestValidator : AbstractValidator<GoogleCompleteProfileRequest>
{
    private const string FullNamePattern = @"^[\p{L}\p{M}]+(?:\s+[\p{L}\p{M}]+)*$";
    private const string PhonePattern = @"^0\d{9}$";

    public GoogleCompleteProfileRequestValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).Matches(FullNamePattern);

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            RuleFor(x => x.PhoneNumber).Matches(PhonePattern));
    }
}
