namespace Backend.Exceptions
{
    public class QuestionValidationException : DetailedValidationException
    {
        public QuestionValidationException(IEnumerable<string> errors)
            : base("Question validation failed.", errors.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList())
        {
        }
    }
}
