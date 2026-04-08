namespace Backend.Constants
{
    public static class QuestionStatus
    {
        public const string Draft = "Draft";
        public const string Active = "Active";
        public const string Archive = "Archive"; 
        public const string Inprogress = "Inprogress"; 

        public static bool IsValid(string status)
        {
            return status == Draft || status == Active || status == Archive || status == Inprogress;
        }
    }
}
