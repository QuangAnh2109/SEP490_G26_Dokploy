using Backend.Common.Models;
using Backend.DTOs.Course;
using Backend.DTOs.ExamBlueprint;

namespace Backend.Services.Interfaces;

public interface ICourseService
{
    // Read-only — no business failure possible at service level
    Task<List<CourseDTO>> GetCoursesForUserAsync(int userId);
    Task<List<CourseDTO>> GetAllAsync();
    Task<List<StudentInClassDTO>> GetStudentsInClassAsync(int classId);
    Task<List<StudentInClassDTO>> GetPendingStudentsAsync(int classId);
    Task<List<SubjectOptionDto>> GetSubjectsAsync();

    // Read with membership check — Result-bound
    Task<Result<List<ExamInCourseDTO>>> GetExamsForCurrentUserAsync(int classId);
    Task<Result<List<ChapterDTO>>> GetChaptersForCurrentUserAsync(int classId);
    Task<Result<CourseDTO>> GetClassSettingsAsync(int classId);

    // Mutating — returns Result
    Task<Result<CourseDTO>> CreateCourseAsync(int teacherId, CreateCourseRequestDTO dto);
    Task<Result> JoinCourseAsync(int studentId, string? inviteCode);
    Task<Result> LeaveCourseAsync(int classId, int userId);
    Task<Result> UpdateClassSettingsAsync(int classId, string? newName, int? invitationStatus);
    Task<Result<InviteStudentResultDTO>> InviteStudentByEmailAsync(int teacherId, int classId, string? studentEmail);
    Task<Result> AcceptInvitationAsync(int studentId, string? token);
    Task<Result> ApproveStudentAsync(int classId, int studentId);
    Task<Result> RejectStudentAsync(int classId, int studentId);
    Task<Result> RemoveStudentAsync(int classId, int studentId);
    Task<Result> CloseClassAsync(int classId);
    Task<Result> ReopenClassAsync(int classId);
}
