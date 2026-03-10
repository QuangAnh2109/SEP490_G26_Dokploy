namespace Backend.Constants
{
    public static class QuestionType
    {
        public const string FillBlank = "FillInBlank";
        public const string Mcq = "MultipleChoice";

        public static bool IsValid(string type)
        {
            return type == FillBlank || type == Mcq;
        }
    }
}
