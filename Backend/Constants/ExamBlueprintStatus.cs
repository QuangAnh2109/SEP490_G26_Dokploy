namespace Backend.Constants
{
    public static class ExamBlueprintStatus
    {
        public const int Draft = 0;
        public const int Active = 1;
        public const int Inprogress = 2;
        public const int Archived = 3;

        public static string GetLabel(int status)
        {
            return status switch
            {
                Draft => "Draft",
                Active => "Active",
                Inprogress => "Inprogress",
                Archived => "Archived",
                _ => "Unknown"
            };
        }
    }
}
