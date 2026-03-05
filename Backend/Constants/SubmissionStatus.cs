namespace Backend.Constants
{
    public static class SubmissionStatus
    {
        /// <summary>Đang làm bài</summary>
        public const int InProgress = 1;

        /// <summary>Đã nộp bài – không thao tác được nữa</summary>
        public const int Submitted = 2;

        public static bool IsValid(int status)
        {
            return status == InProgress || status == Submitted;
        }
    }
}
