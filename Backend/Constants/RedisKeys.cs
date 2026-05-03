namespace Backend.Constants;

public static class RedisKeys
{
    public const int HangfireDb = 1;
    public const int AuthDb = 0;

    public const string AppPrefix = "mtca:";
    public const string HangfirePrefix = $"{AppPrefix}hangfire:";
    public const string ExamJobsPrefix = $"{AppPrefix}exam-jobs";
    public const string RehydratorLockKey = $"{AppPrefix}rehydrator:lock";

    public const string AecRateLimitPrefix = $"{AppPrefix}aec:ratelimit:";

    public const string RefreshTokenPrefix = $"{AppPrefix}rt:";
    public const string RefreshTokenHashPrefix = $"{AppPrefix}rt-hash:";
    public const string RefreshTokenUserPrefix = $"{AppPrefix}rt-user:";

    public const string OtpPrefix = $"{AppPrefix}otp:";
}
