using Backend.DTOs.Curriculum.Chapter;
using FluentValidation;

namespace Backend.Validators.Curriculum;

public class UpdateChapterRequestValidator : AbstractValidator<UpdateChapterRequest>
{
    public UpdateChapterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        When(x => x.DisplayOrder.HasValue, () =>
        {
            RuleFor(x => x.DisplayOrder!.Value).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}
