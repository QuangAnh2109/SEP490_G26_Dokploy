using Backend.Constants;
using Backend.DTOs.Question;
using FluentValidation;

namespace Backend.Validators.Question;

public class QuestionStatusUpdateDtoValidator : AbstractValidator<QuestionStatusUpdateDto>
{
    public QuestionStatusUpdateDtoValidator()
    {
        RuleFor(x => x.QuestionIds)
            .NotEmpty();

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => QuestionStatus.IsValid(s ?? string.Empty));
    }
}
