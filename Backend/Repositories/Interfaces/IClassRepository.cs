using Backend.DTOs.Class;
using Backend.DTOs.ExamBlueprint;
using Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Repositories.Interfaces
{
    public interface IClassRepository
    {
        Task<List<ClassDTO>> GetClassesForUserAsync(int userId);
        Task<List<ClassDTO>> GetAllAsync();
        Task<ClassDTO?> GetByIdAsync(int classId);
        Task<List<ExamInClassDTO>> GetExamsByClassAsync(int classId, bool isTeacher = false);

        Task<string?> GetDuplicateClassErrorAsync(int teacherId, string className, int semesterId, int subjectId);
        Task<ClassDTO> CreateClassAsync(Class newClass);

        Task<Class?> GetClassByInviteCodeAsync(string inviteCode);
        Task<bool> IsUserInClassAsync(int classId, int userId);
        Task JoinClassAsync(int classId, int userId);

        Task<ClassMember?> InviteStudentAsync(int classId, int studentId);
        Task<int> AcceptEmailInvitationAsync(int classId, int studentId, byte[] concurrencyStamp);
        Task<List<StudentInClassDTO>> GetPendingStudentsAsync(int classId);
        Task<bool> ApproveStudentAsync(int classId, int studentId);
        Task<bool> RejectStudentAsync(int classId, int studentId);
        Task<bool> RemoveStudentAsync(int classId, int studentId);

        Task<List<StudentInClassDTO>> GetStudentsInClassAsync(int classId);
        Task LeaveClassAsync(int classId, int userId);
        Task<bool> UpdateClassSettingsAsync(int classId, string newName, int invitationStatus);
        Task<bool> CloseClassAsync(int classId);
        Task<bool> ReopenClassAsync(int classId);
        Task<List<SubjectOptionDto>> GetSubjectsAsync();
        Task<List<SemesterOptionDto>> GetSemesterOptionsAsync();
        Task<(DateOnly StartDate, DateOnly EndDate)?> GetSemesterRangeAsync(int classId);
        
        Task<User?> GetUserWithRoleByEmailAsync(string email);
        Task<ClassMember?> GetClassMemberAsync(int classId, int studentId);
        Task UpdateClassMemberStatusAsync(int classId, int studentId, int status);
        Task<bool> IsTeacherOfClassAsync(int classId, int teacherUserId);
        Task<bool> StudentHasInProgressSubmissionInClassAsync(int classId, int studentId);
        Task<bool> RemoveActiveStudentFromClassAsync(int classId, int studentId);
        Task<int> BulkCloseBySemesterAsync(int semesterId);
    }
}
