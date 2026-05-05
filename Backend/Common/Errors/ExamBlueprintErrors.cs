using Backend.Common.Models;
using Backend.Constants;

namespace Backend.Common.Errors;

public static class ExamBlueprintErrors
{
    public static readonly Error InvalidSubject = new(ErrorCodes.ExamBlueprintInvalidSubject, ErrorType.Validation);
    public static readonly Error SubjectNotFound = new(ErrorCodes.ExamBlueprintSubjectNotFound, ErrorType.NotFound);
    public static readonly Error InvalidBlueprintId = new(ErrorCodes.ExamBlueprintInvalidBlueprintId, ErrorType.Validation);
    public static readonly Error NotFound = new(ErrorCodes.ExamBlueprintNotFound, ErrorType.NotFound);
    public static readonly Error InvalidTargetStatus = new(ErrorCodes.ExamBlueprintInvalidTargetStatus, ErrorType.Validation);
    public static readonly Error ValidationFailed = new(ErrorCodes.Validation, ErrorType.Validation);
    public static readonly Error InsufficientQuestionBank = new(ErrorCodes.ExamBlueprintInsufficientQuestionBank, ErrorType.Validation);
    public static readonly Error DuplicateRow = new(ErrorCodes.ExamBlueprintDuplicateRow, ErrorType.Validation);
    public static readonly Error TargetTotalMismatch = new(ErrorCodes.ExamBlueprintTargetTotalMismatch, ErrorType.Validation);
    public static readonly Error EmptyRows = new(ErrorCodes.ExamBlueprintEmptyRows, ErrorType.Validation);
    public static readonly Error InvalidUpdateStatus = new(ErrorCodes.ExamBlueprintInvalidUpdateStatus, ErrorType.Validation);
    public static readonly Error CannotDelete = new(ErrorCodes.ExamBlueprintCannotDelete, ErrorType.Validation);
    public static readonly Error InUse = new(ErrorCodes.ExamBlueprintInUse, ErrorType.Validation);
    public static readonly Error DuplicateName = new(ErrorCodes.ExamBlueprintDuplicateName, ErrorType.Conflict);
}
