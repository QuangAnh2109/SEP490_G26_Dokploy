using System;

namespace Backend.Exceptions
{
    public class AutoApprovePendingException : Exception
    {
        public AutoApprovePendingException(string message) : base(message)
        {
        }
    }
}
