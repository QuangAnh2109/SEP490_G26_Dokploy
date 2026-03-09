using System.Net;

namespace Backend.Exceptions
{
    public abstract class BaseException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public object? Details { get; }

        protected BaseException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError, object? details = null) 
            : base(message)
        {
            StatusCode = statusCode;
            Details = details;
        }
    }
}
