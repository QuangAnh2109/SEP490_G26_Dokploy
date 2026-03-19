namespace Backend.Constants
{
    public static class SubmissionStatus
    {
        /// <summary>Đang làm bài</summary>
        public const int InProgress = 1;

        /// <summary>Đã nộp bài – không thao tác được nữa</summary>
        public const int Submitted = 2;

        /// <summary>Vắng thi – học sinh không có bài làm nào</summary>
        public const int Absent = 3;

        public static bool IsValid(int status)
        {
            return status == InProgress || status == Submitted;
        }
    }
}
