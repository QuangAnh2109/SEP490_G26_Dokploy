using Backend.Models;

namespace Backend.Repositories.Interfaces;

public interface IAnalyticsRepository
{
    /// <summary>
    /// Lấy Exam kèm toàn bộ graph: Papers → Submissions (+ Student) → StudentAnswers → QuestionAnswer → Question → Chapter, và Papers → Questions → Chapter/QuestionAnswers.
    /// </summary>
    Task<Exam?> GetExamWithFullGraphAsync(int examId);

    /// <summary>
    /// Lấy danh sách học sinh trong lớp (ClassMembers) kèm thông tin Student.
    /// </summary>
    Task<List<ClassMember>> GetClassMembersWithStudentsAsync(int classId);

    /// <summary>
    /// Lấy Submission theo ID kèm Paper (để lấy ExamId).
    /// </summary>
    Task<Submission?> GetSubmissionByIdWithPaperAsync(int submissionId);
}
