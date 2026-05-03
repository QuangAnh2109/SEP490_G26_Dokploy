using Backend.DTOs.Class;
using FluentValidation;

namespace Backend.Validators.Class;

public class UpdateClassSettingsRequestValidator : AbstractValidator<UpdateClassSettingsRequestDTO>
{
    public UpdateClassSettingsRequestValidator()
    {
        RuleFor(x => x.ClassName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.InvitationCodeStatus).NotNull().InclusiveBetween(0, 1);
    }
}
