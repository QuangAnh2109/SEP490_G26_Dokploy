using Backend.Constants;
using Backend.Common.Models;

namespace Backend.Common.Errors;

public static class SubjectErrors
{
    public static readonly Error NotFound = new(ErrorCodes.SubjectNotFound, ErrorType.NotFound);
    public static readonly Error CodeDuplicate = new(ErrorCodes.SubjectCodeDuplicate, ErrorType.Conflict);
    public static readonly Error Closed = new(ErrorCodes.SubjectClosed, ErrorType.Conflict);
    public static readonly Error AlreadyClosed = new(ErrorCodes.SubjectAlreadyClosed, ErrorType.Conflict);
    public static readonly Error ConcurrentUpdate = new(ErrorCodes.SubjectConcurrentUpdate, ErrorType.Conflict);
}
