using Backend.DTOs;
using FluentValidation;

namespace Backend.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private const string FullNamePattern = @"^[\p{L}\p{M}]+(?:\s+[\p{L}\p{M}]+)*$";
    private const string PhonePattern = @"^0\d{9}$";
    private const string StudentIdPattern = @"^[A-Za-z]{2}\d{6}$";
    private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,72}$";

    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).Matches(FullNamePattern);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().Matches(PasswordPattern);
        RuleFor(x => x.RoleId).NotNull().InclusiveBetween(1, 2);

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            RuleFor(x => x.PhoneNumber).Matches(PhonePattern));

        When(x => x.RoleId == 2, () =>
            RuleFor(x => x.StudentId).NotEmpty().Matches(StudentIdPattern));
    }
}
