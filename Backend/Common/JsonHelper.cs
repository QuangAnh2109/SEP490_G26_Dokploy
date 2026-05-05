namespace Backend.Common
{
    public class JsonHelper
    {
        public static string? UnescapeLatex(string? input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Replace(@"\\", @"\");
        }

        public static string? EscapeLatex(string? input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Replace(@"\", @"\\");
        }
    }
}
