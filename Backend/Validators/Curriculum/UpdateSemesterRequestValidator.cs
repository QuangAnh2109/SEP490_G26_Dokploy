using Backend.Constants;
using Backend.DTOs.Curriculum.Semester;
using FluentValidation;

namespace Backend.Validators.Curriculum;

public class UpdateSemesterRequestValidator : AbstractValidator<UpdateSemesterRequest>
{
    public UpdateSemesterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty().GreaterThan(x => x.StartDate)
            .WithErrorCode(ErrorCodes.SemesterDateInvalid);
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
