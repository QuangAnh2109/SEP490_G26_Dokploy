using Backend.Common.Models;
using Backend.Constants;

namespace Backend.Common.Errors;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new(ErrorCodes.AuthInvalidCredentials, ErrorType.NotFound);
    public static readonly Error OtpExpired = new(ErrorCodes.AuthOtpExpired, ErrorType.Unauthorized);
    public static readonly Error OtpInvalid = new(ErrorCodes.AuthOtpInvalid, ErrorType.Unauthorized);
    public static readonly Error InvalidGoogleToken = new(ErrorCodes.AuthInvalidGoogleToken, ErrorType.Unauthorized);
    public static readonly Error UnknownRole = new(ErrorCodes.AuthUnknownRole, ErrorType.Forbidden);
    public static readonly Error UserNotFound = new(ErrorCodes.AuthUserNotFound, ErrorType.NotFound);
    public static readonly Error GoogleAccountNoPassword = new(ErrorCodes.AuthGoogleAccountNoPassword, ErrorType.Forbidden);
    public static readonly Error InvalidRefreshToken = new(ErrorCodes.AuthInvalidRefreshToken, ErrorType.Unauthorized);
    public static readonly Error RefreshTokenNotFound = new(ErrorCodes.AuthRefreshTokenNotFound, ErrorType.Unauthorized);
    public static readonly Error MissingJti = new(ErrorCodes.AuthMissingJti, ErrorType.Unauthorized);
    public static readonly Error PasswordChangeRequired = new(ErrorCodes.AuthPasswordChangeRequired, ErrorType.Forbidden);
    public static readonly Error CurrentPasswordWrong = new(ErrorCodes.AuthCurrentPasswordWrong, ErrorType.Unauthorized);
    public static readonly Error NewPasswordSameAsOld = new(ErrorCodes.AuthNewPasswordSameAsOld, ErrorType.Validation);
    public static readonly Error AccountLocked = new(ErrorCodes.AuthAccountLocked, ErrorType.Forbidden);
}
