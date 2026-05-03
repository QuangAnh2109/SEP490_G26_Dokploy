using Backend.DTOs.Curriculum.Chapter;
using FluentValidation;

namespace Backend.Validators.Curriculum;

public class ReorderChaptersRequestValidator : AbstractValidator<ReorderChaptersRequest>
{
    public ReorderChaptersRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ChapterId).GreaterThan(0);
            item.RuleFor(i => i.DisplayOrder).GreaterThanOrEqualTo(0);
        });
    }
}
