using System.Net;

namespace Backend.Exceptions
{
    public class DetailedValidationException : BaseException
    {
        public List<string> Errors { get; }

        public DetailedValidationException(string message, List<string> errors) 
            : base(message, HttpStatusCode.BadRequest, errors)
        {
            Errors = errors;
        }

        public DetailedValidationException(List<string> errors) 
            : this("Validation failed", errors)
        {
        }
    }
}
