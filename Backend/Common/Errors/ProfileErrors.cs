using Backend.Common.Models;
using Backend.Constants;

namespace Backend.Common.Errors;

public static class ProfileErrors
{
    public static readonly Error NotFound = new(ErrorCodes.ProfileNotFound, ErrorType.NotFound);
    public static readonly Error GoogleAccount = new(ErrorCodes.ProfileGoogleAccount, ErrorType.Forbidden);
    public static readonly Error WrongPassword = new(ErrorCodes.ProfileWrongPassword, ErrorType.Unauthorized);
    public static readonly Error StudentIdRequired = new(ErrorCodes.ProfileStudentIdRequired, ErrorType.Validation);
}
