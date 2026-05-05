using Backend.Constants;
using Backend.Common.Models;

namespace Backend.Common.Errors;

public static class ChapterErrors
{
    public static readonly Error NotFound = new(ErrorCodes.ChapterNotFound, ErrorType.NotFound);
    public static readonly Error NameDuplicate = new(ErrorCodes.ChapterNameDuplicate, ErrorType.Conflict);
    public static readonly Error AlreadyDeleted = new(ErrorCodes.ChapterAlreadyDeleted, ErrorType.Conflict);
    public static readonly Error ConcurrentUpdate = new(ErrorCodes.ChapterConcurrentUpdate, ErrorType.Conflict);
}
