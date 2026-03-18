using System.Collections.Generic;
using System.Threading.Tasks;

using Backend.DTOs.Course;
using Backend.DTOs.Subject;
using Backend.Models;

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
        Task<bool> UpdateClassSettingsAsync(int classId, string newName, int invitationStatus);
        Task<List<SubjectDTO>> GetSubjectsAsync();
    }
}
