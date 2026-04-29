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

    // Auth
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AuthEmailAlreadyRegistered = "AUTH_EMAIL_ALREADY_REGISTERED";
    public const string AuthOtpExpired = "AUTH_OTP_EXPIRED";
    public const string AuthOtpInvalid = "AUTH_OTP_INVALID";
    public const string AuthInvalidGoogleToken = "AUTH_INVALID_GOOGLE_TOKEN";
    public const string AuthUnknownRole = "AUTH_UNKNOWN_ROLE";
    public const string AuthUserNotFound = "AUTH_USER_NOT_FOUND";
    public const string AuthGoogleAccountNoPassword = "AUTH_GOOGLE_ACCOUNT_NO_PASSWORD";
    public const string AuthInvalidRefreshToken = "AUTH_INVALID_REFRESH_TOKEN";

    // Question
    public const string QuestionNotFound = "QUESTION_NOT_FOUND";
    public const string QuestionInUse = "QUESTION_IN_USE";
    public const string QuestionInvalidDeleteStatus = "QUESTION_INVALID_DELETE_STATUS";
    public const string QuestionEmptyList = "QUESTION_EMPTY_LIST";

    // ExamBlueprint
    public const string ExamBlueprintInvalidSubject = "EXAM_BLUEPRINT_INVALID_SUBJECT";
    public const string ExamBlueprintSubjectNotFound = "EXAM_BLUEPRINT_SUBJECT_NOT_FOUND";
    public const string ExamBlueprintInvalidBlueprintId = "EXAM_BLUEPRINT_INVALID_BLUEPRINT_ID";
    public const string ExamBlueprintNotFound = "EXAM_BLUEPRINT_NOT_FOUND";
    public const string ExamBlueprintInvalidTargetStatus = "EXAM_BLUEPRINT_INVALID_TARGET_STATUS";
    public const string ExamBlueprintInsufficientQuestionBank = "EXAM_BLUEPRINT_INSUFFICIENT_QUESTION_BANK";
    public const string ExamBlueprintDuplicateRow = "EXAM_BLUEPRINT_DUPLICATE_ROW";
    public const string ExamBlueprintTargetTotalMismatch = "EXAM_BLUEPRINT_TARGET_TOTAL_MISMATCH";
    public const string ExamBlueprintEmptyRows = "EXAM_BLUEPRINT_EMPTY_ROWS";
    public const string ExamBlueprintInvalidUpdateStatus = "EXAM_BLUEPRINT_INVALID_UPDATE_STATUS";
    public const string ExamBlueprintCannotDelete = "EXAM_BLUEPRINT_CANNOT_DELETE";
    public const string ExamBlueprintInUse = "EXAM_BLUEPRINT_IN_USE";
    public const string ExamBlueprintDuplicateName = "EXAM_BLUEPRINT_DUPLICATE_NAME";
}
