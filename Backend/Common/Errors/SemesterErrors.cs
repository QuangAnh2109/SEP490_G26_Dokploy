using Backend.Common.Models;
using Backend.Constants;

namespace Backend.Common.Errors;

public static class SemesterErrors
{
    public static readonly Error NotFound = new(ErrorCodes.SemesterNotFound, ErrorType.NotFound);
    public static readonly Error CodeDuplicate = new(ErrorCodes.SemesterCodeDuplicate, ErrorType.Conflict);
    public static readonly Error Closed = new(ErrorCodes.SemesterClosed, ErrorType.Conflict);
    public static readonly Error AlreadyClosed = new(ErrorCodes.SemesterAlreadyClosed, ErrorType.Conflict);
    public static readonly Error DateInvalid = new(ErrorCodes.SemesterDateInvalid, ErrorType.Validation);
    public static readonly Error ConcurrentUpdate = new(ErrorCodes.SemesterConcurrentUpdate, ErrorType.Conflict);
}
