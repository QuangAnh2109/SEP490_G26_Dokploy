using Backend.DTOs.Course;
using FluentValidation;

namespace Backend.Validators.Course;

public class UpdateCourseSettingsRequestValidator : AbstractValidator<UpdateCourseSettingsRequestDTO>
{
    public UpdateCourseSettingsRequestValidator()
    {
        RuleFor(x => x.ClassName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.InvitationCodeStatus).NotNull().InclusiveBetween(0, 1);
    }
}
