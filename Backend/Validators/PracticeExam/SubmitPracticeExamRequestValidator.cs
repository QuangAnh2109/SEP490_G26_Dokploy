using Backend.DTOs.PracticeExam;
using FluentValidation;

namespace Backend.Validators.PracticeExam;

public class SubmitPracticeExamRequestValidator : AbstractValidator<SubmitPracticeExamRequest>
{
    public SubmitPracticeExamRequestValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotNull().WithMessage("ID bài luyện tập không được để trống.")
            .GreaterThan(0).WithMessage("ID bài luyện tập không hợp lệ.");

        RuleFor(x => x.StudentAnswers)
            .NotNull().WithMessage("Danh sách câu trả lời không được để trống.");
            
        RuleForEach(x => x.StudentAnswers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.QuestionAnswerId)
                .NotNull().WithMessage("ID câu trả lời không được để trống.")
                .GreaterThan(0).WithMessage("ID câu trả lời không hợp lệ.");
        });
    }
}
