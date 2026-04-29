using Backend.Constants;
using Backend.DTOs.Question;
using FluentValidation;

namespace Backend.Validators.Question;

public class QuestionStatusUpdateDtoValidator : AbstractValidator<QuestionStatusUpdateDto>
{
    public QuestionStatusUpdateDtoValidator()
    {
        RuleFor(x => x.QuestionIds)
            .NotEmpty().WithMessage("Question IDs are required.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => QuestionStatus.IsValid(s ?? string.Empty)).WithMessage("Trạng thái không hợp lệ.");
    }
}
