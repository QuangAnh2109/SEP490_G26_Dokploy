using Backend.Common.Models;
using Backend.Constants;

namespace Backend.Common.Errors;

public static class CourseErrors
{
    public static readonly Error NotFound         = new(ErrorCodes.CourseNotFound,         ErrorType.NotFound);
    public static readonly Error Closed           = new(ErrorCodes.CourseClosed,           ErrorType.Conflict);
    public static readonly Error Duplicate        = new(ErrorCodes.CourseDuplicate,        ErrorType.Conflict);
    public static readonly Error InviteCodeInvalid = new(ErrorCodes.CourseInviteCodeInvalid, ErrorType.NotFound);
    public static readonly Error AlreadyMember    = new(ErrorCodes.CourseAlreadyMember,    ErrorType.Conflict);
    public static readonly Error AlreadyInvited   = new(ErrorCodes.CourseAlreadyInvited,   ErrorType.Conflict);
    public static readonly Error NotMember        = new(ErrorCodes.CourseNotMember,        ErrorType.NotFound);
    public static readonly Error AccessDenied     = new(ErrorCodes.CourseAccessDenied,     ErrorType.Forbidden);
    public static readonly Error StudentNotFound  = new(ErrorCodes.CourseStudentNotFound,  ErrorType.NotFound);
    public static readonly Error UserNotStudent   = new(ErrorCodes.CourseUserNotStudent,   ErrorType.Forbidden);
    public static readonly Error InviteTokenInvalid = new(ErrorCodes.CourseInviteTokenInvalid, ErrorType.NotFound);
    public static readonly Error ConfigError      = new(ErrorCodes.CourseConfigError,      ErrorType.Unexpected);
}
