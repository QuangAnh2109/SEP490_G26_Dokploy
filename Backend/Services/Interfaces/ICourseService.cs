using Backend.DTOs.Course;
using Backend.DTOs.ExamBlueprint;
using Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Services.Interfaces
{
    public interface ICourseService
    {
        Task<List<CourseDTO>> GetCoursesForUserAsync(int userId);
        Task<List<CourseDTO>> GetAllAsync();
        Task<CourseDTO?> GetByIdAsync(int classId);

        // New: service method to get visible exams for a class
        Task<List<ExamInCourseDTO>> GetExamsByClassAsync(int classId);

        Task<CourseDTO> CreateCourseAsync(int teacherId, CreateCourseRequestDTO dto);

        Task JoinCourseAsync(int studentId, string inviteCode);

        Task<List<StudentInClassDTO>> GetStudentsInClassAsync(int classId);
        Task LeaveCourseAsync(int classId, int userId);
        Task UpdateClassSettingsAsync(int classId, string newName, int invitationStatus);

        // Feature: Email Invitation & Approval
        Task<string> InviteStudentByEmailAsync(int teacherId, int classId, string studentEmail);
        Task AcceptInvitationAsync(int studentId, string token);
        Task<List<StudentInClassDTO>> GetPendingStudentsAsync(int classId);
        Task ApproveStudentAsync(int classId, int studentId);
        Task RejectStudentAsync(int classId, int studentId);
        Task<List<SubjectOptionDto>> GetSubjectsAsync();
    }
}
