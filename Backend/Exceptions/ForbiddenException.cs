using System.Net;

namespace Backend.Exceptions
{
    public class ForbiddenException : BaseException
    {
        public ForbiddenException(string message = "Bạn không có quyền truy cập tài nguyên này.") 
            : base(message, HttpStatusCode.Forbidden)
        {
        }
    }
}
