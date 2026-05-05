using Backend.DTOs;
using FluentValidation;

namespace Backend.Validators.Submission;

public class SubmitExamRequestValidator : AbstractValidator<SubmitExamRequest>
{
    public SubmitExamRequestValidator()
    {
        RuleFor(x => x.ExamId)
            .NotNull();

        RuleFor(x => x.Submit)
            .NotNull();

        RuleForEach(x => x.StudentAnswers).SetValidator(new StudentAnswerDtoValidator()!);
    }
}

public class StudentAnswerDtoValidator : AbstractValidator<StudentAnswerDto>
{
    public StudentAnswerDtoValidator()
    {
        RuleFor(x => x.QuestionAnswerId)
            .NotNull();
    }
}
