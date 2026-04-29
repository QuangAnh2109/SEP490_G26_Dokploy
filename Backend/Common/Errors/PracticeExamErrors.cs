using Backend.Common.Models;
using Backend.Constants;

namespace Backend.Common.Errors;

public static class PracticeExamErrors
{
    public static readonly Error ClassNotFound = new(
        ErrorCodes.PracticeExamClassNotFound,
        ErrorType.NotFound);

    public static readonly Error ChapterRequired = new(
        ErrorCodes.PracticeExamChapterRequired,
        ErrorType.Validation);

    public static readonly Error InvalidQuestionCount = new(
        ErrorCodes.PracticeExamInvalidQuestionCount,
        ErrorType.Validation);

    public static readonly Error ChapterNotBelongToSubject = new(
        ErrorCodes.PracticeExamChapterNotBelongToSubject,
        ErrorType.Validation);

    public static readonly Error NoQuestionsFound = new(
        ErrorCodes.PracticeExamNoQuestionsFound,
        ErrorType.Conflict);

    public static readonly Error SubmissionNotFound = new(
        ErrorCodes.PracticeExamSubmissionNotFound,
        ErrorType.NotFound);

    public static readonly Error AlreadySubmitted = new(
        ErrorCodes.PracticeExamAlreadySubmitted,
        ErrorType.Conflict);

    public static readonly Error PaperNotFound = new(
        ErrorCodes.PracticeExamPaperNotFound,
        ErrorType.NotFound);

    public static readonly Error InvalidAnswer = new(
        ErrorCodes.PracticeExamInvalidAnswer,
        ErrorType.Validation);

    public static readonly Error NotSubmitted = new(
        ErrorCodes.PracticeExamNotSubmitted,
        ErrorType.Conflict);
}
