namespace Backend.Constants;

public static class RedisKeys
{
    public const int HangfireDb = 1;

    public const string AppPrefix = "mtca:";
    public const string HangfirePrefix = $"{AppPrefix}hangfire:";
    public const string ExamJobsPrefix = $"{AppPrefix}exam-jobs";
    public const string RehydratorLockKey = $"{AppPrefix}rehydrator:lock";
}
