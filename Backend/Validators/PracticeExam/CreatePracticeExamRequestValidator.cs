using Backend.DTOs.PracticeExam;
using FluentValidation;

namespace Backend.Validators.PracticeExam;

public class CreatePracticeExamRequestValidator : AbstractValidator<CreatePracticeExamRequest>
{
    public CreatePracticeExamRequestValidator()
    {
        RuleFor(x => x.ClassId)
            .NotNull().WithMessage("ID lớp học không được để trống.")
            .GreaterThan(0).WithMessage("ID lớp học không hợp lệ.");

        RuleFor(x => x.ChapterIds)
            .NotNull().WithMessage("Phải chọn ít nhất 1 chương.")
            .NotEmpty().WithMessage("Phải chọn ít nhất 1 chương.");

        RuleFor(x => x.TotalQuestions)
            .NotNull().WithMessage("Số câu hỏi mong muốn không được để trống.")
            .InclusiveBetween(5, 30).WithMessage("Số câu hỏi phải từ 5 đến 30.");
    }
}
