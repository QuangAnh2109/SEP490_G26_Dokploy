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
}
