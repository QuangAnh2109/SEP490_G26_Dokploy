namespace Backend.Constants
{
    public static class ExamBlueprintStatus
    {
        public const int NotStarted = 0;
        public const int Approved = 1;
        public const int InUse = 2;
        public const int Archived = 3;

        public static string GetLabel(int status)
        {
            return status switch
            {
                NotStarted => "Draft",
                Approved => "Approved",
                InUse => "In Use",
                Archived => "Archived",
                _ => "Unknown"
            };
        }
    }
}
