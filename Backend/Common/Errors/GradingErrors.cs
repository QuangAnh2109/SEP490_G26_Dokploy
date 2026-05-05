using Backend.Common.Models;
using Backend.Constants;

namespace Backend.Common.Errors;

public static class GradingErrors
{
    public static readonly Error SubmissionNotFound = new(ErrorCodes.GradingSubmissionNotFound, ErrorType.NotFound);
    public static readonly Error NotEligibleForRegrade = new(ErrorCodes.GradingNotEligibleForRegrade, ErrorType.Validation);
}
