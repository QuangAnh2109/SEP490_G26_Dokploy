using Backend.Constants;
using Backend.Common.Models;

namespace Backend.Common.Errors;

public static class AssignExamErrors
{
    public static readonly Error TeacherNotFound = new(ErrorCodes.AssignExamTeacherNotFound, ErrorType.NotFound);
    public static readonly Error BlueprintNotFound = new(ErrorCodes.AssignExamBlueprintNotFound, ErrorType.NotFound);
    public static readonly Error InsufficientQuestions = new(ErrorCodes.AssignExamInsufficientQuestions, ErrorType.Conflict);
    public static readonly Error ClassNotFound = new(ErrorCodes.AssignExamClassNotFound, ErrorType.NotFound);
    public static readonly Error ClassNotOwnedByTeacher = new(ErrorCodes.AssignExamClassNotOwnedByTeacher, ErrorType.Forbidden);
    public static readonly Error SubjectMismatch = new(ErrorCodes.AssignExamSubjectMismatch, ErrorType.Conflict);
    public static readonly Error NoQuestionsSelected = new(ErrorCodes.AssignExamNoQuestionsSelected, ErrorType.Conflict);
    public static readonly Error ExamNotFound = new(ErrorCodes.AssignExamNotFound, ErrorType.NotFound);
    public static readonly Error PaperNotFound = new(ErrorCodes.AssignExamPaperNotFound, ErrorType.NotFound);
    public static readonly Error QuestionNotFound = new(ErrorCodes.AssignExamQuestionNotFound, ErrorType.NotFound);
    public static readonly Error QuestionNotInPaper = new(ErrorCodes.AssignExamQuestionNotInPaper, ErrorType.Conflict);
    public static readonly Error QuestionInactive = new(ErrorCodes.AssignExamQuestionInactive, ErrorType.Conflict);
    public static readonly Error DifficultyMismatch = new(ErrorCodes.AssignExamDifficultyMismatch, ErrorType.Conflict);
    public static readonly Error ChapterMismatch = new(ErrorCodes.AssignExamChapterMismatch, ErrorType.Conflict);
    public static readonly Error InvalidStatusForCancel = new(ErrorCodes.AssignExamInvalidStatusForCancel, ErrorType.Conflict);
    public static readonly Error ExamAlreadyStarted = new(ErrorCodes.AssignExamAlreadyStarted, ErrorType.Conflict);
    public static readonly Error InvalidStatusForRestore = new(ErrorCodes.AssignExamInvalidStatusForRestore, ErrorType.Conflict);
    public static readonly Error OpenTimePassed = new(ErrorCodes.AssignExamOpenTimePassed, ErrorType.Conflict);
    public static readonly Error DurationMismatch = new(ErrorCodes.AssignExamDurationMismatch, ErrorType.Conflict);
    public static readonly Error InvalidStatusForDelete = new(ErrorCodes.AssignExamInvalidStatusForDelete, ErrorType.Conflict);
    public static readonly Error InvalidStatusForUpdate = new(ErrorCodes.AssignExamInvalidStatusForUpdate, ErrorType.Conflict);
    public static readonly Error InvalidOrInactiveQuestions = new(ErrorCodes.AssignExamInvalidOrInactiveQuestions, ErrorType.Conflict);
    public static readonly Error MultipleSubjects = new(ErrorCodes.AssignExamMultipleSubjects, ErrorType.Conflict);
    public static readonly Error MissingClassId = new(ErrorCodes.AssignExamMissingClassId, ErrorType.Validation);
    public static readonly Error PublicWithClassId = new(ErrorCodes.AssignExamPublicWithClassId, ErrorType.Validation);
    public static readonly Error SwapMissingFields = new(ErrorCodes.AssignExamSwapMissingFields, ErrorType.Validation);
    public static readonly Error ManualEmptyQuestions = new(ErrorCodes.AssignExamManualEmptyQuestions, ErrorType.Validation);
    public static readonly Error InvalidTimeWindow = new(ErrorCodes.AssignExamInvalidTimeWindow, ErrorType.Validation);
    public static readonly Error ConcurrentUpdate = new(ErrorCodes.AssignExamConcurrentUpdate, ErrorType.Conflict);
}
