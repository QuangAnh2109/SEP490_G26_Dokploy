using Backend.Common;
using Backend.Common.Models;
using Backend.Constants;

namespace Backend.Common.Errors;

public static class SubmissionErrors
{
    public static readonly Error NotFound = new(
        ErrorCodes.SubmissionNotFound,
        ErrorType.NotFound);

    public static readonly Error AlreadySubmitted = new(
        ErrorCodes.SubmissionAlreadySubmitted,
        ErrorType.Conflict);

    public static readonly Error InvalidAnswer = new(
        ErrorCodes.SubmissionInvalidAnswer,
        ErrorType.Validation);
}
