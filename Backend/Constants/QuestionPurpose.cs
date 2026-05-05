namespace Backend.Constants
{
    public static class QuestionPurpose
    {
        public const byte Exam = 1;
        public const byte Practice = 2;

        public static bool IsValid(byte purpose)
        {
            return purpose == Exam || purpose == Practice;
        }

        public static string GetLabel(byte purpose)
        {
            return purpose switch
            {
                Exam => "Kiểm tra",
                Practice => "Luyện tập",
                _ => "Không xác định"
            };
        }
    }
}
