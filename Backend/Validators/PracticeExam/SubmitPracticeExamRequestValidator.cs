using Backend.DTOs.PracticeExam;
using FluentValidation;

namespace Backend.Validators.PracticeExam;

public class SubmitPracticeExamRequestValidator : AbstractValidator<SubmitPracticeExamRequest>
{
    public SubmitPracticeExamRequestValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotNull()
            .GreaterThan(0);

        RuleFor(x => x.StudentAnswers)
            .NotNull();

        RuleForEach(x => x.StudentAnswers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.QuestionAnswerId)
                .NotNull()
                .GreaterThan(0);
        });
    }
}
