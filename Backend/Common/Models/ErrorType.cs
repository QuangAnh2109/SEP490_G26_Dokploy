namespace Backend.Common.Models;

public enum ErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Locked,
    TooManyRequests,
    Unexpected
}
