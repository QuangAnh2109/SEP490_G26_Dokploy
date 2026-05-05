using Backend.DTOs.ExamBlueprint;
using FluentValidation;

namespace Backend.Validators.ExamBlueprint;

public class CreateExamBlueprintRequestValidator : AbstractValidator<CreateExamBlueprintRequest>
{
    public CreateExamBlueprintRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.SubjectId)
            .NotNull()
            .GreaterThan(0);

        RuleFor(x => x.TargetStatus)
            .NotNull()
            .Must(s => s == Constants.ExamBlueprintStatus.Draft || s == Constants.ExamBlueprintStatus.Active);

        RuleFor(x => x.TargetTotalQuestions)
            .NotNull()
            .GreaterThanOrEqualTo(0);

        RuleForEach(x => x.Rows).SetValidator(new CreateExamBlueprintRowDtoValidator())
            .When(x => x.Rows != null);
    }
}

public class CreateExamBlueprintRowDtoValidator : AbstractValidator<CreateExamBlueprintRowDto>
{
    public CreateExamBlueprintRowDtoValidator()
    {
        RuleFor(x => x.ChapterId)
            .NotNull()
            .GreaterThan(0);

        RuleFor(x => x.Difficulty)
            .NotNull()
            .InclusiveBetween(1, 4);

        RuleFor(x => x.TotalQuestions)
            .NotNull()
            .GreaterThanOrEqualTo(0);
    }
}

public class BlueprintStatusUpdateDtoValidator : AbstractValidator<BlueprintStatusUpdateDto>
{
    public BlueprintStatusUpdateDtoValidator()
    {
        RuleFor(x => x.ExamBlueprintIds)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.Status)
            .NotNull()
            .Equal(Constants.ExamBlueprintStatus.Archived);
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
