using Backend.DTOs.Course;
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
        Task<bool> UpdateClassSettingsAsync(int classId, string newName, int invitationStatus);
    }
}
