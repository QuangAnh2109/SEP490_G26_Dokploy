using Backend.Constants;
using Backend.Common.Models;

namespace Backend.Common.Errors;

public static class AnalyticsErrors
{
    public static readonly Error ExamNotFound = new(
        ErrorCodes.AnalyticsExamNotFound,
        ErrorType.NotFound);

    public static readonly Error SubmissionNotFound = new(
        ErrorCodes.AnalyticsSubmissionNotFound,
        ErrorType.NotFound);

    public static readonly Error InvalidToken = new(
        ErrorCodes.AnalyticsInvalidToken,
        ErrorType.Unauthorized);
}
