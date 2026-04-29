using Backend.DTOs.Course;
using FluentValidation;

namespace Backend.Validators.Course;

public class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequestDTO>
{
    public CreateCourseRequestValidator()
    {
        RuleFor(x => x.ClassName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SubjectId).NotNull().GreaterThan(0);
        RuleFor(x => x.Semester).NotEmpty().MaximumLength(20);
    }
}
