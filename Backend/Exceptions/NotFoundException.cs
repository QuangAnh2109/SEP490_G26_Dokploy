using System.Net;

namespace Backend.Exceptions
{
    public class NotFoundException : BaseException
    {
        public NotFoundException(string message) 
            : base(message, HttpStatusCode.NotFound)
        {
        }
    }
}
