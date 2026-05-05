using Backend.DTOs.Profile;
using FluentValidation;

namespace Backend.Validators.Profile;

public class UpdateProfileDTOValidator : AbstractValidator<UpdateProfileDTO>
{
    public UpdateProfileDTOValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200)
            .Matches(@"^[\p{L}\p{M}]+(?:\s+[\p{L}\p{M}]+)*$");

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            RuleFor(x => x.PhoneNumber).Matches(@"^0\d{9}$"));

        When(x => !string.IsNullOrWhiteSpace(x.StudentId), () =>
            RuleFor(x => x.StudentId).Matches(@"^[A-Za-z]{2}\d{6}$"));
    }
}
