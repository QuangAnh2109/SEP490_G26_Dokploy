using Backend.Constants;
using Backend.Common.Models;

namespace Backend.Common.Errors;

public static class QuestionErrors
{
    public static readonly Error NotFound = new(
        ErrorCodes.QuestionNotFound,
        ErrorType.NotFound);

    public static readonly Error InUse = new(
        ErrorCodes.QuestionInUse,
        ErrorType.Conflict);

    public static readonly Error InvalidDeleteStatus = new(
        ErrorCodes.QuestionInvalidDeleteStatus,
        ErrorType.Validation);

    public static readonly Error EmptyList = new(
        ErrorCodes.QuestionEmptyList,
        ErrorType.Validation);
}
