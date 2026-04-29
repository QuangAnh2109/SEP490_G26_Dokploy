namespace Backend.Constants;

public static class ErrorCodes
{
    public const string Unexpected = "UNEXPECTED";
    public const string Validation = "VALIDATION";
    public const string BadRequest = "BAD_REQUEST";

    // Profile
    public const string ProfileNotFound = "PROFILE_NOT_FOUND";
    public const string ProfileGoogleAccount = "PROFILE_GOOGLE_ACCOUNT";
    public const string ProfileWrongPassword = "PROFILE_WRONG_PASSWORD";
    public const string ProfileStudentIdRequired = "PROFILE_STUDENT_ID_REQUIRED";

    // Course
    public const string CourseNotFound = "COURSE_NOT_FOUND";
    public const string CourseClosed = "COURSE_CLOSED";
    public const string CourseDuplicate = "COURSE_DUPLICATE";
    public const string CourseInviteCodeInvalid = "COURSE_INVITE_CODE_INVALID";
    public const string CourseAlreadyMember = "COURSE_ALREADY_MEMBER";
    public const string CourseAlreadyInvited = "COURSE_ALREADY_INVITED";
    public const string CourseNotMember = "COURSE_NOT_MEMBER";
    public const string CourseStudentNotFound = "COURSE_STUDENT_NOT_FOUND";
    public const string CourseUserNotStudent = "COURSE_USER_NOT_STUDENT";
    public const string CourseInviteTokenInvalid = "COURSE_INVITE_TOKEN_INVALID";
    public const string CourseConfigError = "COURSE_CONFIG_ERROR";
}
