using Backend.Constants;
using Backend.Common.Models;

namespace Backend.Common.Errors;

public static class StudentExamErrors
{
    public static readonly Error NotFound = new(
        ErrorCodes.StudentExamNotFound,
        ErrorType.NotFound);

    public static readonly Error AnotherActiveSubmission = new(
        ErrorCodes.StudentExamAnotherActiveSubmission,
        ErrorType.Validation);

    public static readonly Error MaxAttemptsReached = new(
        ErrorCodes.StudentExamMaxAttemptsReached,
        ErrorType.Validation);

    public static readonly Error NoPapers = new(
        ErrorCodes.StudentExamNoPapers,
        ErrorType.Validation);

    public static readonly Error NotAllowed = new(
        ErrorCodes.StudentExamNotAllowed,
        ErrorType.Forbidden);
}
