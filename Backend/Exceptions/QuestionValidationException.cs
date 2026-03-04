namespace Backend.Exceptions
{
    public sealed class QuestionValidationException : Exception
    {
        public IReadOnlyList<string> Errors { get; }

        public QuestionValidationException(IEnumerable<string> errors)
            : base("Question validation failed.")
        {
            Errors = errors.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList();
        }
    }
}
