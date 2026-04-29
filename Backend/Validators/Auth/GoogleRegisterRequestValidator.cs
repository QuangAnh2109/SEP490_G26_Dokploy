using Backend.DTOs;
using FluentValidation;

namespace Backend.Validators.Auth;

public class GoogleRegisterRequestValidator : AbstractValidator<GoogleRegisterRequest>
{
    private const string FullNamePattern = @"^[\p{L}\p{M}]+(?:\s+[\p{L}\p{M}]+)*$";
    private const string PhonePattern = @"^0\d{9}$";
    private const string StudentIdPattern = @"^[A-Za-z]{2}\d{6}$";

    public GoogleRegisterRequestValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).Matches(FullNamePattern);
        RuleFor(x => x.RoleId).NotNull().InclusiveBetween(1, 2);

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            RuleFor(x => x.PhoneNumber).Matches(PhonePattern));

        When(x => x.RoleId == 2, () =>
            RuleFor(x => x.StudentId).NotEmpty().Matches(StudentIdPattern));
    }
}
