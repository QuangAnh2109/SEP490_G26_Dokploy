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
    public const string CourseAccessDenied = "COURSE_ACCESS_DENIED";
    public const string CourseStudentNotFound = "COURSE_STUDENT_NOT_FOUND";
    public const string CourseUserNotStudent = "COURSE_USER_NOT_STUDENT";
    public const string CourseInviteTokenInvalid = "COURSE_INVITE_TOKEN_INVALID";
    public const string CourseConfigError = "COURSE_CONFIG_ERROR";

    // Auth
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AuthOtpExpired = "AUTH_OTP_EXPIRED";
    public const string AuthOtpInvalid = "AUTH_OTP_INVALID";
    public const string AuthInvalidGoogleToken = "AUTH_INVALID_GOOGLE_TOKEN";
    public const string AuthUnknownRole = "AUTH_UNKNOWN_ROLE";
    public const string AuthUserNotFound = "AUTH_USER_NOT_FOUND";
    public const string AuthGoogleAccountNoPassword = "AUTH_GOOGLE_ACCOUNT_NO_PASSWORD";
    public const string AuthInvalidRefreshToken = "AUTH_INVALID_REFRESH_TOKEN";
    public const string AuthRefreshTokenNotFound = "AUTH_REFRESH_TOKEN_NOT_FOUND";
    public const string AuthMissingJti = "AUTH_MISSING_JTI";
    public const string AuthPasswordChangeRequired = "AUTH_PASSWORD_CHANGE_REQUIRED";
    public const string AuthCurrentPasswordWrong = "AUTH_CURRENT_PASSWORD_WRONG";
    public const string AuthNewPasswordSameAsOld = "AUTH_NEW_PASSWORD_SAME_AS_OLD";
    public const string AuthAccountLocked = "AUTH_ACCOUNT_LOCKED";

    // Admin User
    public const string AdminUserNotFound           = "ADMIN_USER_NOT_FOUND";
    public const string AdminUserEmailExists        = "ADMIN_USER_EMAIL_EXISTS";
    public const string AdminUserInvalidRole        = "ADMIN_USER_INVALID_ROLE";
    public const string AdminUserCannotLockSelf     = "ADMIN_USER_CANNOT_LOCK_SELF";
    public const string AdminUserCannotModifyAdmin  = "ADMIN_USER_CANNOT_MODIFY_ADMIN";
    public const string AdminUserEmailSendFailed    = "ADMIN_USER_EMAIL_SEND_FAILED";

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

    // AssignExam
    public const string AssignExamTeacherNotFound = "ASSIGN_EXAM_TEACHER_NOT_FOUND";
    public const string AssignExamBlueprintNotFound = "ASSIGN_EXAM_BLUEPRINT_NOT_FOUND";
    public const string AssignExamInsufficientQuestions = "ASSIGN_EXAM_INSUFFICIENT_QUESTIONS";
    public const string AssignExamClassNotFound = "ASSIGN_EXAM_CLASS_NOT_FOUND";
    public const string AssignExamClassNotOwnedByTeacher = "ASSIGN_EXAM_CLASS_NOT_OWNED_BY_TEACHER";
    public const string AssignExamSubjectMismatch = "ASSIGN_EXAM_SUBJECT_MISMATCH";
    public const string AssignExamNoQuestionsSelected = "ASSIGN_EXAM_NO_QUESTIONS_SELECTED";
    public const string AssignExamNotFound = "ASSIGN_EXAM_NOT_FOUND";
    public const string AssignExamPaperNotFound = "ASSIGN_EXAM_PAPER_NOT_FOUND";
    public const string AssignExamQuestionNotFound = "ASSIGN_EXAM_QUESTION_NOT_FOUND";
    public const string AssignExamQuestionNotInPaper = "ASSIGN_EXAM_QUESTION_NOT_IN_PAPER";
    public const string AssignExamQuestionInactive = "ASSIGN_EXAM_QUESTION_INACTIVE";
    public const string AssignExamDifficultyMismatch = "ASSIGN_EXAM_DIFFICULTY_MISMATCH";
    public const string AssignExamChapterMismatch = "ASSIGN_EXAM_CHAPTER_MISMATCH";
    public const string AssignExamInvalidStatusForCancel = "ASSIGN_EXAM_INVALID_STATUS_FOR_CANCEL";
    public const string AssignExamAlreadyStarted = "ASSIGN_EXAM_ALREADY_STARTED";
    public const string AssignExamInvalidStatusForRestore = "ASSIGN_EXAM_INVALID_STATUS_FOR_RESTORE";
    public const string AssignExamOpenTimePassed = "ASSIGN_EXAM_OPEN_TIME_PASSED";
    public const string AssignExamDurationMismatch = "ASSIGN_EXAM_DURATION_MISMATCH";
    public const string AssignExamInvalidStatusForDelete = "ASSIGN_EXAM_INVALID_STATUS_FOR_DELETE";
    public const string AssignExamInvalidStatusForUpdate = "ASSIGN_EXAM_INVALID_STATUS_FOR_UPDATE";
    public const string AssignExamInvalidOrInactiveQuestions = "ASSIGN_EXAM_INVALID_OR_INACTIVE_QUESTIONS";
    public const string AssignExamMultipleSubjects = "ASSIGN_EXAM_MULTIPLE_SUBJECTS";
    public const string AssignExamMissingClassId = "ASSIGN_EXAM_MISSING_CLASS_ID";
    public const string AssignExamPublicWithClassId = "ASSIGN_EXAM_PUBLIC_WITH_CLASS_ID";
    public const string AssignExamSwapMissingFields = "ASSIGN_EXAM_SWAP_MISSING_FIELDS";
    public const string AssignExamManualEmptyQuestions = "ASSIGN_EXAM_MANUAL_EMPTY_QUESTIONS";
    public const string AssignExamInvalidTimeWindow = "ASSIGN_EXAM_INVALID_TIME_WINDOW";
    public const string AssignExamConcurrentUpdate = "EXAM_CONCURRENT_UPDATE";

    // StudentExam
    public const string StudentExamNotFound = "STUDENT_EXAM_NOT_FOUND";
    public const string StudentExamAnotherActiveSubmission = "STUDENT_EXAM_ANOTHER_ACTIVE_SUBMISSION";
    public const string StudentExamMaxAttemptsReached = "STUDENT_EXAM_MAX_ATTEMPTS_REACHED";
    public const string StudentExamNoPapers = "STUDENT_EXAM_NO_PAPERS";
    public const string StudentExamNotAllowed = "STUDENT_EXAM_NOT_ALLOWED";

    // Submission
    public const string SubmissionNotFound = "SUBMISSION_NOT_FOUND";
    public const string SubmissionAlreadySubmitted = "SUBMISSION_ALREADY_SUBMITTED";
    public const string SubmissionLate = "SUBMISSION_LATE";
    public const string SubmissionInvalidAnswer = "SUBMISSION_INVALID_ANSWER";

    // PracticeExam
    public const string PracticeExamClassNotFound = "PRACTICE_EXAM_CLASS_NOT_FOUND";
    public const string PracticeExamChapterRequired = "PRACTICE_EXAM_CHAPTER_REQUIRED";
    public const string PracticeExamInvalidQuestionCount = "PRACTICE_EXAM_INVALID_QUESTION_COUNT";
    public const string PracticeExamChapterNotBelongToSubject = "PRACTICE_EXAM_CHAPTER_NOT_BELONG_TO_SUBJECT";
    public const string PracticeExamNoQuestionsFound = "PRACTICE_EXAM_NO_QUESTIONS_FOUND";
    public const string PracticeExamSubmissionNotFound = "PRACTICE_EXAM_SUBMISSION_NOT_FOUND";
    public const string PracticeExamAlreadySubmitted = "PRACTICE_EXAM_ALREADY_SUBMITTED";
    public const string PracticeExamPaperNotFound = "PRACTICE_EXAM_PAPER_NOT_FOUND";
    public const string PracticeExamInvalidAnswer = "PRACTICE_EXAM_INVALID_ANSWER";
    public const string PracticeExamNotSubmitted = "PRACTICE_EXAM_NOT_SUBMITTED";

    // Analytics
    public const string AnalyticsExamNotFound = "ANALYTICS_EXAM_NOT_FOUND";
    public const string AnalyticsSubmissionNotFound = "ANALYTICS_SUBMISSION_NOT_FOUND";
    public const string AnalyticsInvalidToken = "ANALYTICS_INVALID_TOKEN";
}
