using Backend.Common.Models;
using Backend.Constants;

namespace Backend.Common.Errors;

public static class ClassErrors
{
    public static readonly Error NotFound         = new(ErrorCodes.ClassNotFound,         ErrorType.NotFound);
    public static readonly Error Closed           = new(ErrorCodes.ClassClosed,           ErrorType.Conflict);
    public static readonly Error Duplicate        = new(ErrorCodes.ClassDuplicate,        ErrorType.Conflict);
    public static readonly Error InviteCodeInvalid = new(ErrorCodes.ClassInviteCodeInvalid, ErrorType.NotFound);
    public static readonly Error AlreadyMember    = new(ErrorCodes.ClassAlreadyMember,    ErrorType.Conflict);
    public static readonly Error AlreadyInvited   = new(ErrorCodes.ClassAlreadyInvited,   ErrorType.Conflict);
    public static readonly Error NotMember        = new(ErrorCodes.ClassNotMember,        ErrorType.NotFound);
    public static readonly Error AccessDenied     = new(ErrorCodes.ClassAccessDenied,     ErrorType.Forbidden);
    public static readonly Error StudentNotFound  = new(ErrorCodes.ClassStudentNotFound,  ErrorType.NotFound);
    public static readonly Error UserNotStudent   = new(ErrorCodes.ClassUserNotStudent,   ErrorType.Forbidden);
    public static readonly Error InviteTokenInvalid = new(ErrorCodes.ClassInviteTokenInvalid, ErrorType.NotFound);
    public static readonly Error ConfigError      = new(ErrorCodes.ClassConfigError,      ErrorType.Unexpected);
}
