namespace Backend.Constants
{
    public static class ErrorMessages
    {
        // Auth errors
        public const string InvalidEmailOrPassword = "Invalid email or password";
        public const string UserAlreadyExists = "User already exists";
        public const string UserNotFound = "Tài khoản không tồn tại.";
        public const string EmailAlreadyRegistered = "Email này đã được đăng ký trong hệ thống. Vui lòng sử dụng Email khác.";
        public const string OtpExpiredOrNotExists = "OTP đã hết hạn hoặc không tồn tại.";
        public const string InvalidOtp = "Mã OTP không chính xác.";
        public const string InvalidGoogleToken = "Invalid Google token";
        public const string InvalidGoogleTokenSignature = "Invalid Google token signature";

        // Auth controller processing errors
        public const string LoginProcessingError = "An error occurred while processing the login";
        public const string GoogleLoginProcessingError = "An error occurred while processing the Google login";
        public const string GoogleRegistrationProcessingError = "An error occurred while processing the Google registration";
        public const string SendOtpError = "Lỗi khi gửi OTP";
        public const string AccountCreationError = "Lỗi khi tạo tài khoản";

        // Submission errors
        public const string SubmissionNotFound = "Không tìm thấy bài làm.";
        public const string SubmissionAlreadySubmitted = "Bài làm đã được nộp trước đó.";
        public const string SubmissionLateNotAllowed = "Đã quá thời gian nộp bài.";
        public const string InvalidQuestionAnswer = "Câu trả lời không hợp lệ.";
    }

    public static class SuccessMessages
    {
        public const string OtpSentSuccess = "Mã OTP đã được gửi đến email của bạn.";
    }

    public static class ValidationMessages
    {
        public const string EmailRequired = "Email là bắt buộc.";
        public const string EmailFormatInvalid = "Email không đúng định dạng chuẩn RFC 5322.";
        public const string PasswordRequired = "Mật khẩu là bắt buộc.";
        public const string PasswordLengthInvalid = "Mật khẩu phải từ 8 đến 72 ký tự.";
        public const string PasswordComplexityInvalid = "Mật khẩu bắt buộc bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.";
    }
}
