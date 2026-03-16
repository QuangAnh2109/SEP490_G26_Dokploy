using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Repositories.Interfaces;

public interface IAssignExamRepository
{
    Task<bool> IsUserActiveAsync(int id, CancellationToken ct);
    
    Task<AssignExamFiltersResponseDto> GetAssignExamFilterOptionsAsync(int teacherId, CancellationToken ct);
    
    Task<(List<ClassWithCount> Items, int Total)> GetPagedClassesForTeacherAsync(
        int? teacherId, string? kw, string? subj, string? sem, int page, int size, CancellationToken ct);
        
    Task<List<BlueprintListItemDto>> GetBlueprintsAsync(int? teacherId, string? subj, string? kw, CancellationToken ct);
    
    Task<List<BlueprintDetailRowDto>> GetBlueprintDetailAsync(int id, CancellationToken ct);
    
    Task<List<QuestionListItemDto>> GetQuestionsAsync(
        int? teacherId, string? subj, int? ch, int? diff, string[] activeStatus, CancellationToken ct);
        
    Task<ExamBlueprint?> GetBlueprintWithChaptersAsync(int id, CancellationToken ct);
    
    Task<List<int>> GetQuestionIdsForBlueprintRowAsync(int chapterId, int difficulty, int count, string[] activeStatus, CancellationToken ct);
    
    Task<List<QuestionSubjectDto>> GetQuestionsWithSubjectByIdsAsync(IEnumerable<int> ids, string[] activeStatus, CancellationToken ct);
    
    Task<Class?> GetClassByIdAsync(int id, CancellationToken ct);
    
    Task<Exam> SaveExamAsync(Exam exam, CancellationToken ct);
    
    Task<Paper> SavePaperAsync(Paper paper, CancellationToken ct);
    
    Task AddPaperQuestionsAsync(int paperId, List<int> questionIds, CancellationToken ct);
    
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
    
    Task<Exam?> GetExamReviewDataAsync(int id, CancellationToken ct);
    
    Task<List<QuestionListItemDto>> GetAlternativeQuestionsAsync(
        int subjectId, int difficulty, string[] activeStatus, List<int> excludeIds, CancellationToken ct);
        
    Task SwapPaperQuestionAsync(int paperId, int oldQuestionId, int newQuestionId, CancellationToken ct);
    
    Task<Paper?> GetPaperWithQuestionsAsync(int paperId, CancellationToken ct);
    
    Task<Question?> GetQuestionByIdAsync(int id, CancellationToken ct);
    
    Task UpdateExamStatusAsync(int id, int status, CancellationToken ct);
    
    Task SaveChangesAsync(CancellationToken ct);
}

public record ClassWithCount(int ClassId, string Name, string Semester, string SubjectCode, int MemberCount);
public record QuestionSubjectDto(int QuestionId, int SubjectId);
