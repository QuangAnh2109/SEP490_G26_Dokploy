namespace Backend.Constants
{
    public static class DifficultyLevel
    {
        public const int Recognition = 1;
        public const int Comprehension = 2;
        public const int Application = 3;
        public const int HighApplication = 4;

        public static string GetLabel(int level)
        {
            return level switch
            {
                Recognition => "Nhận biết",
                Comprehension => "Thông hiểu",
                Application => "Vận dụng",
                HighApplication => "Vận dụng cao",
                _ => "Không xác định"
            };
        }

        public static bool IsValid(int level)
        {
            return level >= Recognition && level <= HighApplication;
        }
    }
}
