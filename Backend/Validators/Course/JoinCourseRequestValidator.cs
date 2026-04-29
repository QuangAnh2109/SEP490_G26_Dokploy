using Backend.DTOs.Course;
using FluentValidation;

namespace Backend.Validators.Course;

public class JoinCourseRequestValidator : AbstractValidator<JoinCourseRequestDTO>
{
    public JoinCourseRequestValidator()
    {
        RuleFor(x => x.InvitationCode).NotEmpty();
    }
}
