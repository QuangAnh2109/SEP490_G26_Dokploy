using Backend.Common.Models;
using Backend.DTOs.Class;
using Backend.DTOs.ExamBlueprint;

namespace Backend.Services.Interfaces;

public interface IClassService
{
    // Read-only — no business failure possible at service level
    Task<List<ClassDTO>> GetClassesForUserAsync(int userId);
    Task<List<ClassDTO>> GetAllAsync();
    Task<List<StudentInClassDTO>> GetStudentsInClassAsync(int classId);
    Task<List<StudentInClassDTO>> GetPendingStudentsAsync(int classId);
    Task<List<SubjectOptionDto>> GetSubjectsAsync();
    Task<List<SemesterOptionDto>> GetSemestersAsync();

    // Read with membership check — Result-bound
    Task<Result<List<ExamInClassDTO>>> GetExamsForCurrentUserAsync(int classId);
    Task<Result<List<ChapterDTO>>> GetChaptersForCurrentUserAsync(int classId);
    Task<Result<ClassDTO>> GetClassSettingsAsync(int classId);

    // Mutating — returns Result
    Task<Result<ClassDTO>> CreateClassAsync(int teacherId, CreateClassRequestDTO dto);
    Task<Result> JoinClassAsync(int studentId, string? inviteCode);
    Task<Result> LeaveClassAsync(int classId, int userId);
    Task<Result> UpdateClassSettingsAsync(int classId, string? newName, int? invitationStatus);
    Task<Result<InviteStudentResultDTO>> InviteStudentByEmailAsync(int teacherId, int classId, string? studentEmail);
    Task<Result> AcceptInvitationAsync(int studentId, string? token);
    Task<Result> ApproveStudentAsync(int classId, int studentId);
    Task<Result> RejectStudentAsync(int classId, int studentId);
    Task<Result> RemoveStudentAsync(int classId, int studentId);
    Task<Result> CloseClassAsync(int classId);
    Task<Result> ReopenClassAsync(int classId);
}
