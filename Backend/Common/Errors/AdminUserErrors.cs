using Backend.Common.Models;
using Backend.Constants;

namespace Backend.Common.Errors;

public static class AdminUserErrors
{
    public static readonly Error NotFound          = new(ErrorCodes.AdminUserNotFound,          ErrorType.NotFound);
    public static readonly Error EmailExists       = new(ErrorCodes.AdminUserEmailExists,       ErrorType.Conflict);
    public static readonly Error InvalidRole       = new(ErrorCodes.AdminUserInvalidRole,       ErrorType.Validation);
    public static readonly Error CannotLockSelf    = new(ErrorCodes.AdminUserCannotLockSelf,    ErrorType.Validation);
    public static readonly Error CannotModifyAdmin = new(ErrorCodes.AdminUserCannotModifyAdmin, ErrorType.Forbidden);
    public static readonly Error EmailSendFailed   = new(ErrorCodes.AdminUserEmailSendFailed,   ErrorType.Unexpected);
}
