using Backend.DTOs.ExamBlueprint;
using FluentValidation;

namespace Backend.Validators.ExamBlueprint;

public class CreateExamBlueprintRequestValidator : AbstractValidator<CreateExamBlueprintRequest>
{
    public CreateExamBlueprintRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên ma trận đề là bắt buộc.")
            .MaximumLength(200).WithMessage("Tên ma trận đề không được vượt quá 200 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.SubjectId)
            .NotNull().WithMessage("Môn học là bắt buộc.")
            .GreaterThan(0).WithMessage("Môn học không hợp lệ.");

        RuleFor(x => x.TargetStatus)
            .NotNull().WithMessage("Trạng thái mục tiêu là bắt buộc.")
            .Must(s => s == Constants.ExamBlueprintStatus.Draft || s == Constants.ExamBlueprintStatus.Active)
            .WithMessage("Trạng thái mục tiêu không hợp lệ.");

        RuleFor(x => x.TargetTotalQuestions)
            .NotNull().WithMessage("Tổng số câu mục tiêu là bắt buộc.")
            .GreaterThanOrEqualTo(0).WithMessage("Tổng số câu mục tiêu không được âm.");

        RuleForEach(x => x.Rows).SetValidator(new CreateExamBlueprintRowDtoValidator())
            .When(x => x.Rows != null);
    }
}

public class CreateExamBlueprintRowDtoValidator : AbstractValidator<CreateExamBlueprintRowDto>
{
    public CreateExamBlueprintRowDtoValidator()
    {
        RuleFor(x => x.ChapterId)
            .NotNull().WithMessage("Chương là bắt buộc.")
            .GreaterThan(0).WithMessage("Mỗi dòng ma trận phải có chương hợp lệ.");

        RuleFor(x => x.Difficulty)
            .NotNull().WithMessage("Mức độ là bắt buộc.")
            .InclusiveBetween(1, 4).WithMessage("Mức độ phải từ 1 đến 4.");

        RuleFor(x => x.TotalQuestions)
            .NotNull().WithMessage("Số câu là bắt buộc.")
            .GreaterThanOrEqualTo(0).WithMessage("Số câu trong từng dòng không được âm.");
    }
}

public class BlueprintStatusUpdateDtoValidator : AbstractValidator<BlueprintStatusUpdateDto>
{
    public BlueprintStatusUpdateDtoValidator()
    {
        RuleFor(x => x.ExamBlueprintIds)
            .NotNull().WithMessage("ExamBlueprintIds are required.")
            .NotEmpty().WithMessage("ExamBlueprintIds are required.");

        RuleFor(x => x.Status)
            .NotNull().WithMessage("Trạng thái là bắt buộc.")
            .Equal(Constants.ExamBlueprintStatus.Archived).WithMessage("Chỉ hỗ trợ chuyển trạng thái sang Lưu trữ.");
    }
}

public class BlueprintListQueryDtoValidator : AbstractValidator<BlueprintListQueryDto>
{
    public BlueprintListQueryDtoValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).When(x => x.Page.HasValue);
            
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).When(x => x.PageSize.HasValue);
    }
}
