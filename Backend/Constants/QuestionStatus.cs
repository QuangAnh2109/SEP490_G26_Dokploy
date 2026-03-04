namespace Backend.Constants
{
    public static class QuestionStatus
    {
        public const string Draft = "Draft";
        public const string Active = "Active";
        public const string Archived = "Archived";

        public static bool IsValid(string status)
        {
            return status == Draft || status == Active || status == Archived;
        }
    }
}
